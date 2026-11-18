# Factory Simulation

An interactive 3D Unity demo that follows one wooden sheet through a cutting station, a manual assembly bench, and a finished-speaker station.

## Controls

| Control | Action |
| --- | --- |
| **Start**, **Pause**, **Resume**, or **Space** | Start or pause the current batch |
| **Reset batch** or **R** | Return the line to its starting state and keep the completed count |
| **Speed** | Cycle through 1×, 2×, and 4× speed |
| **Next batch** or **Space** | Feed another sheet after a batch finishes |

## Production concept

1. **Feed and cut.** A `3 × 3 m` wooden sheet enters the twin-blade station. Two parallel cuts produce three equal `1 × 3 m` panels.
2. **Transfer.** The panels move along the roller line to the assembly bench.
3. **Glue and assemble.** Three workers apply wood glue and join the panels to form the front and two sides of a speaker cabinet.
4. **Output.** The cabinet moves to the display station and the finished-cabinet counter increments.

The demo visualizes the woodworking sequence and the shape of the finished cabinet. A batch takes about **29 seconds** at normal speed.

## Open the Unity project

1. In Unity Hub, install **Unity 2021.3.12f1**. Include **Windows Build Support** if you plan to build the Windows player.
2. In Unity Hub, select **Add → Add project from disk**, then select the [UnityProject](UnityProject) folder.
3. Wait for Unity to import the assets. In the **Project** window, open **Assets → Scenes → FactorySimulation**.
4. Select **Play** in the Unity Editor, then select **Start** in the Game view.

To rebuild the player, open **File → Build Settings**, select **Windows, Mac, Linux**, set the target to **Windows** and the architecture to **x86_64**, and choose **Build**. The `FactorySimulation` scene is already enabled in Build Settings.

## Repository layout

| Folder | Contents |
| --- | --- |
| [UnityProject](UnityProject) | Editable Unity assets, package manifests, and project settings |

## التشغيل والفكرة بالعربي

افتح مجلد UnityProject في Unity Hub بإصدار 2021.3.12f1، ثم افتح مشهد FactorySimulation واضغط Play وبعدها Start.

المصنع بيقسّم لوح خشب **٣ × ٣ متر** إلى ثلاث قطع **١ × ٣ متر**. بعد النقل، ثلاثة عمال بيلزقوا القطع بالغراء عشان يعملوا واجهة السماعة وجانبيها. عند وصولها لمحطة الخروج، عدّاد الإنتاج بيزيد واحد. لتعديل المشهد، افتح مجلد `UnityProject` في Unity Hub بإصدار **2021.3.12f1**.
