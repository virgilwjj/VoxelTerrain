using UnityEngine;

namespace VoxelTerrain
{
    public class WireFrameCamera : MonoBehaviour
    {
        Camera _camera;
        bool isInWireframeMode;
        bool previousMode;
        CameraClearFlags originClearFlags;
        Color originBackgroundColor;

        void Start()
        {
            _camera = Camera.main;
            isInWireframeMode = false;
            previousMode = isInWireframeMode;
            originClearFlags = _camera.clearFlags;
            originBackgroundColor = _camera.backgroundColor;

            Camera.onPreRender += OnPreRenderCallback;
            Camera.onPostRender += OnPostRenderCallback;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                isInWireframeMode = !isInWireframeMode;
            }
            
            if (isInWireframeMode == previousMode)
            {
                return;
            }

            previousMode = isInWireframeMode;
            if (isInWireframeMode)
            {
                originClearFlags = _camera.clearFlags;
                originBackgroundColor = _camera.backgroundColor;
                _camera.clearFlags = CameraClearFlags.Color;
                _camera.backgroundColor = Color.black;
            }
            else
            {
                _camera.clearFlags = originClearFlags;
                _camera.backgroundColor = originBackgroundColor;
            }
        }

        void OnPreRenderCallback(Camera camera)
        {
            if (isInWireframeMode)
            {
                GL.wireframe = true;
            }
        }

        void OnPostRenderCallback(Camera camera)
        {
            if (isInWireframeMode)
            {
                GL.wireframe = false;
            }
        }

    }

}
