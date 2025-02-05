using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
// using UnityEditor.Perception.Visualizer;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if MOQ_PRESENT
using Moq;
using Moq.Protected;
using UnityEngine.Rendering;
#endif

namespace EditorTests
{
    [TestFixture]
    public class PerceptionCameraEditorTests
    {
        [UnityTest]
        public IEnumerator EditorPause_DoesNotLogErrors()
        {
            var cameraObject = SetupCamera(p =>
            {
                var idLabelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
                p.captureRgbImages = true;
                p.AddLabeler(new BoundingBox2DLabeler(idLabelConfig));
                p.AddLabeler(new RenderedObjectInfoLabeler(idLabelConfig));
            });
            cameraObject.name = "Camera";

            PerceptionSettings.instance.endpoint = new PerceptionEndpoint();

            yield return new EnterPlayMode();

            var cam = GameObject.Find("Camera");
            Assert.NotNull(cam);
            var perceptionCamera = cam.GetComponent<PerceptionCamera>();
            Assert.NotNull(perceptionCamera);

            var expectedFirstFrame = Time.frameCount;
            yield return null;
            EditorApplication.isPaused = true;
            //Wait a few editor frames to ensure the issue has a chance to trigger.
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            EditorApplication.isPaused = false;
            var expectedLastFrame = Time.frameCount;
            yield return null;

            var endpoint = DatasetCapture.currentSimulation.consumerEndpoint as PerceptionEndpoint;

            var capturesPath = endpoint != null ? endpoint.datasetPath : string.Empty;

            Assert.IsFalse(string.IsNullOrEmpty(capturesPath));

            DatasetCapture.ResetSimulation();
            var capturesJson = File.ReadAllText(Path.Combine(capturesPath, "captures_000.json"));
            for (var iFrameCount = expectedFirstFrame; iFrameCount <= expectedLastFrame; iFrameCount++)
            {
                StringAssert.Contains($"rgb_{iFrameCount}", capturesJson);
            }

            yield return new ExitPlayMode();
        }
        /**
        [Test, Ignore("Some bug to fix in the test itself")]
        public void VisualizerInstalledSuccessfully()
        {
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            InstallVisualizerAsync();
            // check if visualizer is installed to the correct path
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif
            
#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif
            string path =  Path.Combine(packagesPath, "datasetvisualizer.exe");
            Assert.IsTrue(File.Exists(path));
        }
        
        static void InstallVisualizerAsync()
        {
            // check if visualizer is installed to the correct path
#if UNITY_EDITOR_WIN
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets", "/Library/PythonInstall/Scripts"));
#elif UNITY_EDITOR_OSX
            var packagesPath = Path.GetFullPath(Application.dataPath.Replace("/Assets","/Library/PythonInstall/bin"));
#endif
            
#if UNITY_EDITOR_WIN
            packagesPath = packagesPath.Replace("/", "\\");
#endif
            //==============================INSTALL VISUALIZER IN PYTHON FOR UNITY======================================
            const int steps = 3;
            var exitCode = 0;
            EditorUtility.DisplayProgressBar("Setting up the Visualizer", "Installing Visualizer (This may take a few minutes)", 2f / steps);
            Task.WaitAll(VisualizerInstaller.InstallationCommand(ref exitCode, packagesPath));
            if (exitCode != 0)
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            EditorUtility.ClearProgressBar();
            Debug.Log("Successfully installed visualizer tool!");
        }
        **/

#if MOQ_PRESENT
        [UnityTest]
        public IEnumerator AddLabelerAfterStart_ShouldInitialize()
        {
            yield return new EnterPlayMode();
            var camera = SetupCamera(null);
            var mockLabeler = new Mock<CameraLabeler>();
            yield return null;
            camera.GetComponent<PerceptionCamera>().AddLabeler(mockLabeler.Object);
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator Labeler_ShouldRunCallbacksInFirstFrame()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var camera = SetupCamera(null);
            camera.GetComponent<PerceptionCamera>().AddLabeler(mockLabeler.Object);
            yield return null;

            mockLabeler.Protected().Verify("Setup", Times.Once());
            mockLabeler.Protected().Verify("OnUpdate", Times.Once());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Once(), ItExpr.IsAny<ScriptableRenderContext>());
            mockLabeler.Protected().Verify("OnEndRendering", Times.Once(), ItExpr.IsAny<ScriptableRenderContext>());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator Labeler_ShouldNotRunCallbacksWhenCameraDisabled()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var camera = SetupCamera(null);
            var perceptionCamera = camera.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            yield return null;
            perceptionCamera.enabled = false;
            yield return null;

            mockLabeler.Protected().Verify("Setup", Times.Once());
            mockLabeler.Protected().Verify("OnUpdate", Times.Once());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Once(), ItExpr.IsAny<ScriptableRenderContext>());
            mockLabeler.Protected().Verify("OnEndRendering", Times.Once(), ItExpr.IsAny<ScriptableRenderContext>());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator AddAndRemoveLabelerInSameFrame_ShouldDoNothing()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            perceptionCamera.RemoveLabeler(mockLabeler.Object);
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Never());
            mockLabeler.Protected().Verify("OnUpdate", Times.Never());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Never(), It.IsAny<ScriptableRenderContext>());
            mockLabeler.Protected().Verify("OnEndRendering", Times.Never(), It.IsAny<ScriptableRenderContext>());
            mockLabeler.Protected().Verify("Cleanup", Times.Never());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator RemoveLabeler_ShouldCallCleanup()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            yield return null;
            Assert.IsTrue(perceptionCamera.RemoveLabeler(mockLabeler.Object));
            mockLabeler.Protected().Verify("Cleanup", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator RemoveLabeler_OnLabelerNotAdded_ShouldNotCallCleanup()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            yield return null;
            Assert.IsFalse(perceptionCamera.RemoveLabeler(mockLabeler.Object));
            mockLabeler.Protected().Verify("Cleanup", Times.Never());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator DestroyPerceptionCameraObject_ShouldCallCleanup()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            yield return null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            mockLabeler.Protected().Verify("Cleanup", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator SetupThrows_ShouldDisable()
        {
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            mockLabeler.Protected().Setup("Setup").Throws<InvalidOperationException>();
            var labeler = mockLabeler.Object;
            var camera = SetupCamera(null);
            camera.GetComponent<PerceptionCamera>().AddLabeler(labeler);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Operation is not valid due to the current state of the object.");
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Once());
            mockLabeler.Protected().Verify("OnUpdate", Times.Never());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Never(), It.IsAny<ScriptableRenderContext>());
            mockLabeler.Protected().Verify("OnEndRendering", Times.Never(), It.IsAny<ScriptableRenderContext>());
            Assert.IsFalse(labeler.enabled);
            yield return new ExitPlayMode();
        }
#endif

        static GameObject SetupCamera(Action<PerceptionCamera> initPerceptionCameraCallback)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

#if HDRP_PRESENT
            cameraObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
#elif URP_PRESENT
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
#endif

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            initPerceptionCameraCallback?.Invoke(perceptionCamera);

            cameraObject.SetActive(true);
            return cameraObject;
        }
    }
}
