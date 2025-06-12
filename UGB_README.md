![Ultimate Glove Ball Banner](./Documentation/Media/banner.png "Ultimate Glove Ball")

# Ultimate Glove Ball

Ultimate Glove Ball was built by the VR Developer Tools team to demonstrate how you can quickly build an ESport game that gets people together in VR using the Oculus Social Platform API.
Based off our [SharedSpaces](https://github.com/oculus-samples/Unity-SharedSpaces) project we expand functionalities in an ESport game context. We also demonstrate how VR games can have asymmetric experiences. In this project we have players and spectators.

This codebase is available both as a reference and as a template for multiplayer VR games.

See the [CONTRIBUTING](./CONTRIBUTING.md) file for how to help out.

This project was built using the Unity engine with [Photon Realtime](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime) as the transport layer and [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects).

You can test the game out on [Meta Horizon Store - Ultimate Glove Ball](https://www.meta.com/en-gb/experiences/ultimate-glove-ball/5704438046269164/).

## Project Description

This project is an application for the Meta Quest devices that demonstrate a fast pace sport game that can be played with friends or strangers. It shows how to integrate connection between users joining random games or specific rooms, invite friends and group launch a party in the same arena, or join as a spectator to a game already in progress. We also integrated Meta Avatars for players to represent their VR persona and voice chat for easy communication.

The project also includes the [Meta Utilities](./Packages/com.meta.utilities/README.md) and [Meta Input Utilities](./Packages/com.meta.utilities.input/README.md) packages, which contain many useful tools and methods.

## How to run the project in Unity

1. [Configure the project](./Documentation/Configuration.md) with Meta Quest and Photon
2. Make sure you're using _Unity 2022.3.52f1_ or newer.
3. Load the [Assets/PongHub/Scenes/Startup](./Assets/PongHub/Scenes/Startup.unity) scene.
4. There are two ways of testing in the editor:
   <details>
     <summary><b>Quest Link</b></summary>

   - Enable Quest Link:
     - Put on your headset and navigate to "Quick Settings"; select "Quest Link" (or "Quest Air Link" if using Air Link).
     - Select your desktop from the list and then select, "Launch". This will launch the Quest Link app, allowing you to control your desktop from your headset.
   - With the headset on, select "Desktop" from the control panel in front of you. You should be able to see your desktop in VR!
   - Navigate to Unity and press "Play" - the application should launch on your headset automatically.
   </details>
   <details>
     <summary><b>XR FPS Simulator</b></summary>

   - In Unity, press "Play" and enjoy the simulated XR controls!
   - Review the [XR FPS Simulator documentation](./Packages/com.meta.utilities.input/README.md#xr-device-fps-simulator) for more information. + Note: The mouse is [captured by the simulator](./Packages/com.meta.utilities.input/README.md#mouse-capture) when in play mode. In order to otherwise use the mouse in-game (such as to interact with menus), hold Left Alt.
   </details>

## Dependencies

This project makes use of the following plugins and software:

- [Unity](https://unity.com/download) 2022.3.52f1 or newer
- [Dependencies Hunter](https://github.com/AlexeyPerov/Unity-Dependencies-Hunter.git#upm)
- [Meta Avatars SDK](https://developer.oculus.com/downloads/package/meta-avatars-sdk/)
- [Meta XR Utilities](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.utilities)
- [Oculus Integration SDK](https://developer.oculus.com/downloads/package/unity-integration): released under the _[Oculus SDK License Agreement](./Assets/Oculus/LICENSE.txt)_.
- [ParrelSync](https://github.com/brogan89/ParrelSync)
- [Photon Realtime for Netcode](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime)
- [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518)
- [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- [Unity Toolbar Extender](https://github.com/marijnz/unity-toolbar-extender.git)

The following is required to test this project within Unity:

- [The Oculus App](https://www.oculus.com/setup/)

# Getting the code

First, ensure you have Git LFS installed by running this command:

```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:

```sh
git clone https://github.com/oculus-samples/Unity-UltimateGloveBall.git
```

# Documentation

More information can be found in the [Documentation](./Documentation) section of this project.

- [Avatars](./Documentation/Avatars.md)
- [Ball Physics And Networking](./Documentation/BallPhysicsAndNetworking.md)
- [Code Structure](./Documentation/CodeStructure.md)
- [Configuration](./Documentation/Configuration.md)
- [In-app Purchases(IAP)](./Documentation/IAP.md)
- [Light Baking](./Documentation/LightBaking.md)
- [Multiplayer](./Documentation/Multiplayer.md)

Custom Packages:

- [Meta Multiplayer for Netcode and Photon](./Packages/com.meta.multiplayer.netcode-photon/README.md)
- [Meta Utilities](./Packages/com.meta.utilities/README.md)
- [Meta Input Utilities](./Packages/com.meta.utilities.input/README.md)

# Where are the Meta Avatar SDK and Photon packages?

In order to keep the project organized, the [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) package is stored in the [Packages](./Packages) folder. To update them, import their updated Asset Store packages, then copy them into their respective `Packages` folders.

The _Photon Voice 2_ package is released under the _[License Agreement for Exit Games Photon](./Packages/Photon/Photon/license.txt)_.

Also, the [Photon Realtime for Netcode](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime) package is copied in the [Packages](./Packages) folder as `com.community.netcode.transport.photon-realtime@b28923aa5d` since we modified it to fit our needs.
