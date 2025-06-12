# 代码结构

该项目分为两个主要结构。首先是 [Meta Multiplayer for Netcode and Photon](../Packages/com.meta.multiplayer.netcode-photon) 包，这是核心可重用代码，可以轻松用于启动使用类似多人游戏配置的新项目。然后是 [PongHub](../Assets/PongHub)，它使用 Meta Multiplayer 基础并实现特定的游戏逻辑。

我们还有一个通用实用功能包，帮助我们加快项目实现速度，这些实用工具可以在 [Packages/com.meta.utilities](../Packages/com.meta.utilities) 中找到。

我们还需要扩展 Photon Realtime for Netcode 的功能，为此我们在 [Packages/com.community.netcode.transport.photon-realtime](../Packages/com.community.netcode.transport.photon-realtime@b28923aa5d) 中制作了包的副本。

## Meta Multiplayer for Netcode and Photon

与项目无关的逻辑，可以在任何项目中重用。它实现了网络多人项目所需的不同元素。它还包含我们平台社交 API 的一些关键功能实现。

**[BlockUserManager.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/BlockUserManager.cs)** 实现了屏蔽流程 API。

**[GroupPresenceState.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/GroupPresenceState.cs)** 实现了群组状态 API 的使用，这是玩家轻松一起游戏的基础。

**[NetworkLayer.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/NetworkLayer.cs)** 实现了客户端/主机连接流程和断开连接处理以及主机迁移的网络状态。

网络化身的实现是在项目中集成个性的关键，也是化身如何轻松集成到项目中的好例子（[Avatars](../Packages/com.meta.multiplayer.netcode-photon/Avatar)）。

## PongHub

这是游戏特定内容的实现。我将重点介绍一些关键组件，但强烈建议您深入研究代码。

### 应用程序

应用程序通过 [UGBApplication](../Assets/PongHub/Scripts/App/UGBApplication.cs) 脚本启动。这是为应用程序的生命周期实例化主要系统的地方。它实现了通过应用程序导航的核心、网络逻辑的处理、用户群组状态的设置以及决定最初加载用户的位置。

在 [PongHub/Scripts/App](../Assets/PongHub/Scripts/App) 中，您将找到应用程序核心元素的实现。

### 主菜单

我们要讨论的下一个元素是 [MainMenu 目录](../Assets/PongHub/Scripts/MainMenu)。它包含在主菜单场景中使用的控制器和视图。它的设置方式可以让我们轻松扩展菜单数量和它们之间的导航。[MainMenuController.cs](../Assets/PongHub/Scripts/MainMenu/MainMenuController.cs) 是场景的核心逻辑，处理所有状态和转换以及与核心服务的通信。

### 竞技场

最后，最大的部分是 [Arena 目录](../Assets/PongHub/Scripts/Arena)。这包含了我们进入竞技场时的所有游戏逻辑。

#### 服务

由于我们有两种加入竞技场的模式（玩家或观众），我们需要一种方法来确保给定角色的用户不会超过最大数量加入房间，这就是 [ArenaApprovalController](../Assets/PongHub/Scripts/Arena/Services/ArenaApprovalController.cs) 实现的功能。

然后我们需要在正确的位置和正确的团队中生成玩家，这由 [ArenaPlayerSpawningManager](../Assets/PongHub/Scripts/Arena/Services/ArenaPlayerSpawningManager.cs) 处理。

#### 玩家

一旦生成，玩家的构造就很有趣，因为它由多个网络对象组成。有玩家化身，这是玩家的核心元素，然后我们生成手套骨架和手套本身，因为它们在网络级别上单独交互，但都是玩家实体的一部分。这些可以在 [Scripts/Arena/Player](../Assets/PongHub/Scripts/Arena/Player) 中找到。

#### 观众

在观众模式下，我们选择保持网络简单并重用群众身体和动画。用户确实可以控制在其角色上显示的物品，他们还控制烟花发射器。为了减少网络事件，当观众更改其物品时，我们会等待一定时间再将其同步到服务器，这样如果用户快速更改物品，我们就不会向服务器发送大量事件。

观众代码可以在 [Scripts/Arena/Spectator](../Assets/PongHub/Scripts/Arena/Spectator) 中找到。

#### 球

最后，主要的网络元素是 [球](../Assets/PongHub/Scripts/Arena/Balls)。球分为 3 个主要组件：球网络、球状态同步器和特定的球行为。

球网络处理球的不同网络状态，如所有权、生成或死亡状态，以及游戏逻辑，如碰撞和服务器与客户端 RPC 的抛球。

球状态同步器处理球的位置和运动，您可以在[球物理和网络](./BallPhysicsAndNetworking.md)文档中阅读更多详细信息。

## Photon Realtime Transport for Netcode

我们修改了这个包，以便在导航 photon 房间时有更多的灵活性。我们添加了对连接到大厅的支持，并在尝试连接时有不同的意图。

我们添加的另一个功能是集成在连接时使用房间属性的可能性，这样我们就可以加入特定类型的房间。我们以一种易于在项目之间重用的方式实现它，通过分配回调和数据处理程序将特定实现的权力交给游戏。
