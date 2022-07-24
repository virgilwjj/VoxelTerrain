using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    public class WireFrameViewer : MonoBehaviour
    {
        [SerializeField]
        Color _wireframeBackgroundColor = Color.black;

        bool _isInWireframeMode;
        bool _previousMode;

        Camera _camera;
        CameraClearFlags _originCameraClearFlags;
        Color _originBackgroundColor;

        void Awake()
        {
            _isInWireframeMode = false;
            _previousMode = _isInWireframeMode;

            _camera = GetComponent<Camera>();
            _originCameraClearFlags = _camera.clearFlags;
            _originBackgroundColor = _camera.backgroundColor;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                _isInWireframeMode = !_isInWireframeMode;
            }

            if (_isInWireframeMode == _previousMode)
            {
                return;
            }

            _previousMode = _isInWireframeMode;
            if (_isInWireframeMode)
            {
                _originCameraClearFlags = _camera.clearFlags;
                _originBackgroundColor = _camera.backgroundColor;
                _camera.clearFlags = CameraClearFlags.Color;
                _camera.backgroundColor = _wireframeBackgroundColor;
            }
            else
            {
                _camera.clearFlags = _originCameraClearFlags;
                _camera.backgroundColor = _originBackgroundColor;
            }
        }

        void OnPreRender()
        {
            if (_isInWireframeMode)
            {
                GL.wireframe = true;
            }
        }

        void OnPostRender()
        {
            if (_isInWireframeMode)
            {
                GL.wireframe = false;
            }
        }

    }

}
