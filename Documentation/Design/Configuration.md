# 项目配置

为了使该项目在编辑器和设备上正常运行，需要进行一些初始设置。

## 应用程序配置

为了运行项目并使用平台服务，我们需要在[Meta Quest开发者中心](https://developers.meta.com/horizon/)上创建一个应用程序。

要在设备上运行，您需要一个Quest应用程序；要在编辑器中运行，您需要一个Rift应用程序。以下部分将描述应用程序运行所需的配置。

### 数据使用检查

要使用平台的功能，我们需要请求应用程序所需的数据类型。这可以在应用程序的_数据使用检查_部分找到。

![数据使用检查](./Media/dashboard/datausecheckup.png "数据使用检查")

并配置所需的数据使用：

- **用户ID**：化身、目标位置、多人游戏、Oculus用户名、好友邀请、用户邀请他人
- **应用内购买**：IAP
- **用户资料**：化身
- **化身**：化身
- **深度链接**：目标位置
- **好友**：多人游戏
- **被屏蔽用户**：其他 - 我们使用屏蔽API
- **邀请**：多人游戏、好友邀请、用户邀请他人

### 附加组件

该应用程序集成了应用内购买(IAP)来演示如何集成耐用品和消耗品购买。以下是该应用程序的预期配置。

首先，我们需要从平台服务中打开附加组件：

![平台服务附加组件](./Media/dashboard/dashboard_addons_platformservices.png "平台服务附加组件")

然后我们需要设置不同的附加组件。

![附加组件](./Media/dashboard/dashboard_addons.png "附加组件")

我们创建了3个耐用品SKU，意味着只能购买一次，用于图标。
还有1个消耗品SKU，意味着可以多次购买，用于宠物猫。

### 目标位置

该应用程序使用目标位置配置，使用户能够在相同竞技场中邀请朋友并一起启动应用程序。

首先，我们需要从平台服务中打开目标位置：

![平台服务目标位置](./Media/dashboard/dashboard_destinations_platformservices.png "平台服务目标位置")

然后我们需要设置不同的目标位置。

![目标位置](./Media/dashboard/dashboard_destinations.png "目标位置")

#### 主菜单

首先，我们有主菜单目标位置，这是用户独有的目标位置，其他玩家无法加入。设置如下：

![目标位置主菜单](./Media/dashboard/dashboard_destination_mainmenu.png "目标位置主菜单")

您会注意到深度链接类型设置为DISABLED，这防止用户一起加入此目标位置。

#### 竞技场

然后我们为每个区域设置了不同的竞技场：

![目标位置北美](./Media/dashboard/dashboard_destination_na.png "目标位置北美")

这是北美区域的示例。深度链接类型设置为ENABLE，我们使用深度链接的数据来指定要使用的Photon区域。深度链接消息的格式是我们项目特有的。您还可以注意到设置了群组启动容量。由于竞技场中最多有6名玩家，最大值设置为6。（注意必须设置最大值才能启用群组启动。）

以下是目标位置设置表：

| 目标位置 | API名称 | 深度链接消息 |
|---------|---------|-------------|
| **主菜单** | MainMenu | _不适用_ |
| **北美** | Arena | {"Region":"usw"} |
| **南美** | ArenaSA | {"Region":"sa"} |
| **日本** | ArenaJP | {"Region":"jp"} |
| **欧洲** | ArenaEU | {"Region":"eu"} |
| **澳大利亚** | ArenaAU | {"Region":"au"} |
| **亚洲** | ArenaAsia | {"Region":"asia"} |

### 设置应用程序ID

然后我们需要在Unity项目中设置应用程序ID。

标识符(应用程序ID)可以在_API_部分找到。

![应用程序API](./Media/dashboard/dashboard_api.png "应用程序API")

然后需要将其放置在 [Assets/Resources/OculusPlatformSettings.asset](Assets/Resources/OculusPlatformSettings.asset) 中

![Oculus平台设置菜单](./Media/editor/oculusplatformsettings_menu.png "Oculus平台设置菜单")

![Oculus平台设置](./Media/editor/oculusplatformsettings.png "Oculus平台设置")

## Photon配置

要使示例正常工作，您需要使用自己的账户和应用程序配置Photon。Photon基础计划是免费的。

- 访问 [photonengine.com](https://www.photonengine.com) 并[创建账户](https://doc.photonengine.com/en-us/realtime/current/getting-started/obtain-your-app-id)
- 从您的Photon仪表板，点击"创建新应用程序"
  - 我们将创建2个应用程序，"Realtime"和"Voice"
- 首先填写表单，确保将类型设置为"Photon Realtime"。然后点击创建。
- 其次填写表单，确保将类型设置为"Photon Voice"。然后点击创建。

您的新应用程序现在将显示在Photon仪表板上。点击应用程序ID以显示完整字符串并复制每个应用程序的值。

打开Unity项目并将Realtime应用程序ID粘贴到 [Assets/Photon/Resources/PhotonAppSettings](Assets/Photon/Resources/PhotonAppSettings.asset) 中。

![Photon应用程序设置位置](./Media/editor/photonappsettings_location.png "Photon应用程序设置位置")

![Photon应用程序设置](./Media/editor/photonappsettings.png "Photon应用程序设置")

在VoiceRecorder预制体上设置Voice应用程序ID：

![Photon语音录制器位置](./Media/editor/photonvoicerecorder_location.png "Photon语音录制器位置")

![Photon语音设置](./Media/editor/photonvoicesetting.png "Photon语音设置")

### Realtime的额外设置

在我们的项目中，我们希望玩家能够在不同区域中以较低延迟进行游戏，因此我们使用了Photon提供的不同区域。但为了使项目更简单，我们限制了将使用的区域数量。为此，我们需要将应用程序使用的区域列入白名单。

![Photon白名单](./Media/photon_whitelist.png "Photon白名单")

Photon Realtime传输现在应该可以工作。您可以检查Photon账户中的仪表板以验证是否有网络流量。

## 上传到发布频道

要使用平台功能，您首先需要将初始构建版本上传到发布频道。

有关说明，您可以访问[开发者中心](https://developers.meta.com/horizon/resources/publish-release-channels-upload/)。然后为了能够与其他用户进行测试，您需要将他们添加到频道中，更多信息请参见[将用户添加到发布频道](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/)主题。

一旦上传了初始构建版本，您就能够使用具有相同应用程序ID的任何开发构建版本，无需上传每个构建版本来测试本地更改。
