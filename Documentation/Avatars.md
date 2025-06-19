# 化身系统

为了增强自我意识并营造更强的社交感受，我们在该项目中集成了Meta化身系统。

能够重用平台化身在平台上创造了连续性，用户可以在不同应用程序之间相互识别。

您可以在packages目录中找到Meta Avatar SDK([Packages/Avatar2](../Packages/Avatar2))。
它是从[开发者网站](https://developer.oculus.com/downloads/package/meta-avatars-sdk)下载的。

对于集成，我们遵循了[开发者网站上强调的信息](https://developer.oculus.com/documentation/unity/meta-avatars-overview/)。
[AvatarEntity.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarEntity.cs) 实现是您将看到我们如何为身体、唇同步、面部和眼部追踪设置化身的地方。
此设置还与 [PlayerAvatarEntity Prefab](../Assets/PongHub/Prefabs/Arena/Player/PlayerAvatarEntity.prefab) 相关联，该预制体包含我们在游戏中如何使用化身的所有行为和设置。

为了保持化身与用户位置同步，我们追踪Camera Rig根节点。

有关面部和眼部追踪的更多信息可以在[这里](https://developer.oculus.com/documentation/unity/meta-avatars-face-eye-pose/)找到。

## 网络同步

由于我们正在构建多人游戏，因此需要为化身实现网络解决方案。

这在 [AvatarNetworking.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarNetworking.cs) 中完成。在此实现中，
我们使用化身实体上的 `RecordStreamData` 函数来获取要通过网络传输的数据。然后我们通过RPC发送它，然后被每个其他客户端接收。

在接收端，我们使用 `ApplyStreamData` 函数应用数据，该函数将正确应用化身的状态。
此外，我们实现了发送不同详细级别(LOD)的频率，以便我们可以减少带宽，同时仍保持化身运动的良好保真度。

## 自定义着色器

虽然我们希望保持与Avatar SDK中提供的着色器相同的视觉效果，但我们需要为模型添加一些效果。为此，我们将着色器文件复制到我们的项目中([这里](../Assets/PongHub/VFX/Shaders/CustomAvatar))，以便应用一些修改。

为了最小化原始着色器文件和我们的自定义文件之间的差异，我们在 `.cginc` 文件中实现了新功能，然后引用它们。

**[AvatarGhostEffect.cginc](../Assets/PongHub/VFX/Shaders/CustomAvatar/Horizon/AvatarGhostEffect.cginc)**：此效果在玩家拿取幽灵球时触发，它通过在网格上切割小孔以及添加菲涅尔效果和颜色来创造透明的错觉。

**[AvatarDisolveEffect.cginc](../Assets/PongHub/VFX/Shaders/CustomAvatar/Horizon/AvatarDisolveEffect.cginc)**：此效果在玩家生成或消失时触发。它通过使用Alpha裁剪和着色来创造网格溶解的错觉。

这两个效果的主要函数在 [app_functions.hlsl](../Assets/PongHub/VFX/Shaders/CustomAvatar/app_specific/app_functions.hlsl) 中调用，并通过关键字触发。着色器文件也被修改为包含这些效果工作所需的属性。

我们在复制的文件中保持最小的更改，以便在需要更新时，可以轻松地使用更新版本的Meta Avatar SDK中的最新着色器更新着色器文件。

更多信息可以在相关的[自定义化身README](../Assets/PongHub/VFX/Shaders/CustomAvatar/README.md)中找到。
