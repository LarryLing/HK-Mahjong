import { Unity, useUnityContext } from "react-unity-webgl";

export default function App() {
  // Configured to point directly at your unhashed Unity build outputs
  const { unityProvider, isLoaded, loadingProgression } = useUnityContext({
    loaderUrl: "unity-build/Build/unity-build.loader.js",
    dataUrl: "unity-build/Build/unity-build.data",
    frameworkUrl: "unity-build/Build/unity-build.framework.js",
    codeUrl: "unity-build/Build/unity-build.wasm",
  });

  return (
    <div
      style={{
        width: "100vw",
        height: "100vh",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        backgroundColor: "#1e1f22", // Matches Discord's dark theme background
        overflow: "hidden",
        position: "relative",
      }}
    >
      {!isLoaded && (
        <div
          style={{
            position: "absolute",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            color: "#dbdee1",
            fontFamily: "sans-serif",
          }}
        >
          <div
            style={{
              marginBottom: "10px",
              fontSize: "18px",
              fontWeight: "bold",
            }}
          >
            Loading Game...
          </div>
          <div
            style={{
              width: "200px",
              height: "6px",
              backgroundColor: "#4e5058",
              borderRadius: "3px",
              overflow: "hidden",
            }}
          >
            <div
              style={{
                width: `${Math.round(loadingProgression * 100)}%`,
                height: "100%",
                backgroundColor: "#5865f2", // Discord Blurple color
                transition: "width 0.1s ease-out",
              }}
            />
          </div>
        </div>
      )}
      <Unity
        unityProvider={unityProvider}
        style={{
          width: "100%",
          height: "100%",
          maxWidth: "1280px",
          maxHeight: "720px",
          aspectRatio: "16/9",
          visibility: isLoaded ? "visible" : "hidden", // Keeps a blank canvas from showing early
        }}
      />
    </div>
  );
}
