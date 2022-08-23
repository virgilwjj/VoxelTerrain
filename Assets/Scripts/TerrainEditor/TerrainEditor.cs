using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelTerrain
{
    public class TerrainEditor : MonoBehaviour
    {
        EditorMode _editorMode;
        EditorMode _previousMode;
        WireFrameCamera _wireFrameCamera;
        CameraController _cameraController;
        BrushEditor _brushEditor;
        ModelImporter _modelImporter;
        ChunkManager _chunkManager;

        void Awake()
        {
            _editorMode = EditorMode.View;
            _previousMode = _editorMode;
            _wireFrameCamera = GetComponent<WireFrameCamera>();
            _cameraController = GetComponent<CameraController>();  
            _brushEditor = GetComponent<BrushEditor>();
            _modelImporter = GetComponent<ModelImporter>();
            _cameraController.IsActive = true;
            _brushEditor.IsActive = false;

            _chunkManager = GetComponent<ChunkManager>();
        }

        void Update()
        {
            SwitchMode();
        }

        void SwitchMode()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _editorMode = EditorMode.View;
                
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _editorMode = EditorMode.Edit;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _editorMode = EditorMode.Model;
            }

            if (_previousMode == _editorMode)
            {
                return;
            }
            _previousMode = _editorMode;

            if (_editorMode == EditorMode.View)
            {
                _cameraController.IsActive = true;
            }
            else
            {
                _cameraController.IsActive = false;
            }

            if (_editorMode == EditorMode.Edit)
            {
                _brushEditor.IsActive = true;
            }
            else
            {
                _brushEditor.IsActive = false;
            }

            if (_editorMode == EditorMode.Model)
            {
                _modelImporter.IsActive = true;
            }
            else
            {
                _modelImporter.IsActive = false;
            }
        }

    }

}
