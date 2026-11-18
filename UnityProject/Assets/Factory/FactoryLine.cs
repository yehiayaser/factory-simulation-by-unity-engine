using System;
using UnityEngine;

namespace FactorySimulation
{
    public sealed class FactoryLine : MonoBehaviour
    {
        public const float SheetSize = 3f;
        public const float PanelWidth = SheetSize / 3f;
        public const float Thickness = 0.10f;
        public const float CuttingHeight = 1.05f;
        public const float BenchHeight = 1f;
        public static readonly Vector3 FeedPosition = new Vector3(-7.2f, CuttingHeight + Thickness / 2f, -3.45f);
        public static readonly Vector3 CutPosition = new Vector3(-7.2f, CuttingHeight + Thickness / 2f, -0.45f);
        public static readonly Vector3 BenchPosition = new Vector3(-0.65f, BenchHeight, -0.32f);
        public static readonly Vector3 OutputPosition = new Vector3(6.7f, BenchHeight, -0.32f);

        private static readonly float[] StageEnds = { 3f, 7.3f, 12.1f, 17.5f, 24.3f, 29.3f };
        private static readonly string[] StageNames = { "Feed", "Cut", "Transfer", "Apply glue", "Assemble", "Output" };
        private static readonly string[] StageDescriptions =
        {
            "Feed one 3 x 3 m sheet onto the cutting bed.",
            "Two cutters make parallel cuts to produce three equal 1 x 3 m panels.",
            "Move the three panels from the outfeed onto the assembly bench.",
            "The three workers apply wood glue to the mating edges.",
            "The workers raise and press the front and two sides together.",
            "Move the assembled cabinet to the output station."
        };

        [Header("Cutting and assembly")]
        public Transform sheet;
        public Transform cuttingBridge;
        public Transform[] kerfs;
        public Transform[] panels;
        public Transform[] glueBeads;
        public Transform speakerFace;
        public Operator[] operators;
        public Renderer statusLamp;
        public Camera overviewCamera;

        [Serializable]
        public sealed class Operator
        {
            public Transform root;
            public Transform upperLeft;
            public Transform lowerLeft;
            public Transform upperRight;
            public Transform lowerRight;
            public Transform leftHand;
            public Transform rightHand;
            public Transform[] legs;
            public Transform[] boots;
            public Transform glueBottle;
        }

        private float batchTime;
        private bool running;
        private int completed;
        private int speed = 1;
        private MaterialPropertyBlock lampProperties;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;

        private static float BatchDuration => StageEnds[StageEnds.Length - 1];

        private void Start()
        {
            ApplyFrame(0f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) ToggleRunning();
            if (Input.GetKeyDown(KeyCode.R)) ResetBatch();

            if (running)
            {
                batchTime = Mathf.Min(batchTime + Time.deltaTime * speed, BatchDuration);
                if (batchTime >= BatchDuration)
                {
                    running = false;
                    completed++;
                }

                ApplyFrame(batchTime);
            }

            overviewCamera.orthographicSize = Mathf.Max(7.15f, 12.5f / overviewCamera.aspect);
        }

        private void ToggleRunning()
        {
            if (batchTime >= BatchDuration) ResetBatch();
            running = !running;
        }

        private void ResetBatch()
        {
            running = false;
            batchTime = 0f;
            ApplyFrame(0f);
        }

        private void ApplyFrame(float time)
        {
            float feed = Ease(Fraction(time, 0));
            float cut = Ease(Fraction(time, 1));
            float transfer = Fraction(time, 2);
            float glue = Fraction(time, 3);
            float applyGlue = Mathf.InverseLerp(0f, .76f, glue);
            float build = Fraction(time, 4);
            float output = Ease(Fraction(time, 5));
            bool cutting = time >= StageEnds[0] && time < StageEnds[1];
            bool split = time >= StageEnds[1];
            bool gluing = time >= StageEnds[2] && time < StageEnds[3];
            bool assembling = time >= StageEnds[3] && time < StageEnds[4];

            sheet.gameObject.SetActive(!split);
            sheet.position = Vector3.Lerp(FeedPosition, CutPosition, feed);
            cuttingBridge.position = new Vector3(CutPosition.x, 0f, CutPosition.z + Mathf.Lerp(-1.66f, 1.66f, cut));
            for (int i = 0; i < kerfs.Length; i++)
            {
                float length = Mathf.Clamp(3.32f * cut - 0.16f, 0f, SheetSize);
                kerfs[i].gameObject.SetActive(cutting && length > 0f);
                kerfs[i].position = CutPosition + new Vector3(i - .5f, Thickness / 2f + .001f, (length - SheetSize) / 2f);
                kerfs[i].localScale = new Vector3(.024f, .003f, length);
            }

            Vector3 productPivot = Vector3.Lerp(BenchPosition, OutputPosition, output);
            Quaternion productYaw = Quaternion.Euler(0f, 14f * output, 0f);
            for (int i = 0; i < panels.Length; i++)
            {
                int side = i - 1;
                Transform panel = panels[i];
                panel.gameObject.SetActive(split);
                Vector3 cutCenter = CutPosition + Vector3.right * side * PanelWidth;
                Vector3 transferLane = CutPosition + new Vector3(side * 1.12f, 0f, 2.68f);
                Vector3 aboveBench = BenchPosition + new Vector3(side * 1.13f, .16f, 2.68f);
                Vector3 flatCenter = BenchPosition + new Vector3(side * 1.13f, Thickness / 2f, 0f);
                Vector3 transported = Transfer(cutCenter, transferLane, aboveBench, flatCenter, transfer);

                float lift = Ease(Mathf.InverseLerp(side == 0 ? .04f : .23f + i * .035f, .86f, build));
                float sideYaw = 90f * side * Ease(Mathf.InverseLerp(.58f, .93f, lift));
                Vector3 standingOffset = side == 0
                    ? new Vector3(0f, SheetSize / 2f, -.5f)
                    : new Vector3(side * (PanelWidth - Thickness) / 2f, SheetSize / 2f, .05f);
                Vector3 joinedCenter = productPivot + productYaw * standingOffset;
                Vector3 center = Vector3.Lerp(transported, joinedCenter, lift);
                if (side != 0)
                {
                    float closeJoint = Ease(Mathf.InverseLerp(.72f, 1f, lift));
                    center.x = Mathf.Lerp(transported.x, joinedCenter.x, closeJoint) + side * .16f * Mathf.Sin(lift * Mathf.PI);
                }

                if (lift > 0f && lift < 1f)
                {
                    float angle = lift * Mathf.PI / 2f;
                    center.y = BenchHeight + SheetSize / 2f * Mathf.Sin(angle) + Thickness / 2f * Mathf.Cos(angle);
                }

                panel.SetPositionAndRotation(center,
                    productYaw * Quaternion.Euler(0f, sideYaw, 0f)
                    * Quaternion.Euler(-90f * lift, 0f, 0f));
            }

            ShowGlue(applyGlue, gluing || assembling && build < .83f);
            PositionOperators(glue, applyGlue, build, gluing, assembling);
            speakerFace.gameObject.SetActive(time >= StageEnds[4]);
            speakerFace.SetPositionAndRotation(productPivot + productYaw * new Vector3(0f, SheetSize / 2f, -.552f), productYaw);

            if (lampProperties == null) lampProperties = new MaterialPropertyBlock();
            Color lamp = cutting ? new Color(1f, .48f, .08f) : new Color(.18f, .9f, .63f);
            lampProperties.SetColor("_Color", lamp);
            lampProperties.SetColor("_EmissionColor", lamp * .45f);
            statusLamp.SetPropertyBlock(lampProperties);
        }

        private void ShowGlue(float progress, bool visible)
        {
            for (int i = 0; i < glueBeads.Length; i++)
            {
                int panelIndex = i == 0 ? 0 : i == 3 ? 2 : 1;
                bool outer = panelIndex != 1;
                float length = progress * (outer ? 2.55f : .95f);
                float localX = i < 2 ? -.44f : .44f;
                Transform bead = glueBeads[i];
                bead.gameObject.SetActive(visible && length > .005f);
                bead.SetPositionAndRotation(
                    panels[panelIndex].TransformPoint(new Vector3(localX, .5f + .04f, (-1.28f + length / 2f) / SheetSize)),
                    panels[panelIndex].rotation);
                bead.localScale = new Vector3(.035f, .009f, length);
            }
        }

        private void PositionOperators(float glue, float applyGlue, float build, bool gluing, bool assembling)
        {
            float returnToIdle = gluing ? Ease(Mathf.InverseLerp(.76f, 1f, glue))
                : assembling ? Ease(Mathf.InverseLerp(.87f, 1f, build)) : 1f;
            for (int i = 0; i < operators.Length; i++)
            {
                int side = i - 1;
                Operator worker = operators[i];
                Vector3 idleStation = BenchPosition + (side == 0 ? new Vector3(0f, -BenchHeight, -2.05f) : new Vector3(side * 2.15f, -BenchHeight, -.95f));
                Vector3 idleReach = BenchPosition + new Vector3(side * 1.13f, .14f, -1.25f);
                Vector3 station = idleStation;
                Vector3 reach = idleReach;

                if (gluing)
                {
                    float travel = Mathf.Lerp(-1.28f, 1.27f, applyGlue);
                    float centerReach = Mathf.Lerp(-1.28f, -.34f, applyGlue);
                    reach = side == 0
                        ? BenchPosition + new Vector3(Mathf.Lerp(-.44f, .44f, Ease(applyGlue)), .14f, centerReach)
                        : BenchPosition + new Vector3(side * 1.57f, .14f, travel);
                    if (side != 0) station.z = reach.z - .1f;
                    else station.z = reach.z - .80f;
                }
                else if (assembling)
                {
                    Transform board = panels[i];
                    float gather = Ease(Mathf.InverseLerp(0f, .77f, build));
                    Vector3 target = board.position + new Vector3(side * .24f, 0f, -.25f);
                    reach = Vector3.Lerp(reach, target, gather);
                    reach.y = Mathf.Clamp(reach.y, 1.08f, 1.68f);
                    station.x = Mathf.Lerp(station.x, reach.x + side * .74f, gather);
                    station.z = Mathf.Lerp(station.z, reach.z - (side == 0 ? .65f : .26f), gather);
                }

                station = Vector3.Lerp(station, idleStation, returnToIdle);
                reach = Vector3.Lerp(reach, idleReach, returnToIdle);
                worker.root.position = station;
                Vector3 direction = reach - station;
                direction.y = 0f;
                if (direction.sqrMagnitude > .001f) worker.root.rotation = Quaternion.LookRotation(direction);
                Vector3 localReach = worker.root.InverseTransformPoint(reach);
                Vector3 rightHand = Vector3.Lerp(localReach + Vector3.right * .15f, new Vector3(.28f, .98f, .12f), returnToIdle);
                Vector3 restingLeft = new Vector3(-.27f, 1f, .12f);
                Vector3 workingLeft = assembling ? localReach - Vector3.right * .17f : new Vector3(-.27f, 1f, .38f);
                Vector3 leftHand = Vector3.Lerp(workingLeft, restingLeft, returnToIdle);
                PoseArm(worker.upperLeft, worker.lowerLeft, worker.leftHand, new Vector3(-.26f, 1.53f, 0f), leftHand, -1f);
                PoseArm(worker.upperRight, worker.lowerRight, worker.rightHand, new Vector3(.26f, 1.53f, 0f), rightHand, 1f);
                worker.glueBottle.gameObject.SetActive(gluing && returnToIdle < .64f);
                worker.glueBottle.localPosition = rightHand + new Vector3(0f, .08f, .035f);

                float stride = Mathf.Sin((glue + build) * 32f) * .12f * (1f - returnToIdle);
                for (int leg = 0; leg < 2; leg++)
                {
                    int legSide = leg == 0 ? -1 : 1;
                    Vector3 foot = new Vector3(legSide * .14f, .075f, legSide * stride);
                    worker.boots[leg].localPosition = foot + Vector3.forward * .055f;
                    PlaceLimb(worker.legs[leg], new Vector3(foot.x, .87f, 0f), foot, .20f);
                }
            }
        }

        private static void PoseArm(Transform upper, Transform lower, Transform hand, Vector3 shoulder, Vector3 target, float side)
        {
            Vector3 elbow = Vector3.Lerp(shoulder, target, .52f) + new Vector3(side * .09f, -.13f, -.03f);
            PlaceLimb(upper, shoulder, elbow, .15f);
            PlaceLimb(lower, elbow, target, .125f);
            hand.localPosition = target;
        }

        private static void PlaceLimb(Transform limb, Vector3 from, Vector3 to, float diameter)
        {
            Vector3 delta = to - from;
            limb.localPosition = (from + to) / 2f;
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
            limb.localScale = new Vector3(diameter, delta.magnitude / 2f, diameter);
        }

        private static Vector3 Transfer(Vector3 from, Vector3 lane, Vector3 benchLane, Vector3 bench, float progress)
        {
            if (progress < .36f) return Vector3.Lerp(from, lane, Ease(progress / .36f));
            if (progress < .72f) return Vector3.Lerp(lane, benchLane, Ease(Mathf.InverseLerp(.36f, .72f, progress)));
            return Vector3.Lerp(benchLane, bench, Ease(Mathf.InverseLerp(.72f, 1f, progress)));
        }

        private static float Fraction(float time, int stage)
        {
            return Mathf.InverseLerp(stage == 0 ? 0f : StageEnds[stage - 1], StageEnds[stage], time);
        }

        private static float Ease(float value) => Mathf.SmoothStep(0f, 1f, value);

        private void OnGUI()
        {
            CreateStyles();
            float scale = Mathf.Clamp(Screen.width / 1120f, .53f, 1.45f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 originalMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * originalMatrix;

            int stage = Array.FindIndex(StageEnds, end => batchTime < end);
            if (stage < 0) stage = StageEnds.Length - 1;
            Color ink = new Color(.04f, .08f, .11f, .97f);
            Color accent = new Color(.98f, .71f, .29f);
            Color muted = new Color(.21f, .32f, .36f);
            Fill(new Rect(14f, 14f, width - 28f, 69f), ink);
            Fill(new Rect(14f, 14f, 5f, 69f), accent);
            GUI.Label(new Rect(31f, 22f, width - 260f, 32f), "Factory simulation", titleStyle);
            GUI.Label(new Rect(32f, 54f, width - 260f, 20f), "3 x 3 m sheet  /  3 panels, 1 x 3 m each  /  Speaker cabinet", smallStyle);
            GUI.Label(new Rect(width - 185f, 24f, 157f, 24f), "FINISHED CABINETS", smallStyle);
            GUI.Label(new Rect(width - 185f, 44f, 150f, 33f), completed.ToString("00"), titleStyle);

            float footer = height - 135f;
            Fill(new Rect(14f, footer, width - 28f, 121f), ink);
            float stageWidth = (width - 58f) / StageNames.Length;
            for (int i = 0; i < StageNames.Length; i++)
            {
                Rect tab = new Rect(24f + i * stageWidth, footer + 10f, stageWidth - 3f, 25f);
                Fill(tab, i == stage ? accent : muted);
                GUI.contentColor = i == stage ? Color.black : Color.white;
                GUI.Label(new Rect(tab.x + 8f, tab.y, tab.width - 11f, tab.height), StageNames[i], buttonStyle);
            }
            GUI.contentColor = Color.white;
            Fill(new Rect(24f, footer + 39f, width - 48f, 3f), muted);
            Fill(new Rect(24f, footer + 39f, (width - 48f) * batchTime / BatchDuration, 3f), accent);
            string description = batchTime >= BatchDuration ? "Cabinet complete. Start the next batch when ready." : StageDescriptions[stage];
            GUI.Label(new Rect(24f, footer + 47f, width - 48f, 24f), description, textStyle);

            string action = running ? "Pause [Space]" : batchTime >= BatchDuration ? "Next batch [Space]" : batchTime > 0f ? "Resume [Space]" : "Start [Space]";
            if (GUI.Button(new Rect(24f, footer + 78f, 155f, 31f), action)) ToggleRunning();
            if (GUI.Button(new Rect(187f, footer + 78f, 131f, 31f), "Reset batch [R]")) ResetBatch();
            if (GUI.Button(new Rect(326f, footer + 78f, 103f, 31f), $"Speed {speed}x")) speed = speed == 1 ? 2 : speed == 2 ? 4 : 1;
            GUI.matrix = originalMatrix;
        }

        private void CreateStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            buttonStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
