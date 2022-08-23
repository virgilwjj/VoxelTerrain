using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VoxelTerrain
{
    public class BrushEditor : MonoBehaviour
    {
        public bool IsActive = false;
        [SerializeField]
        TextMeshProUGUI _text;

        BrushTools _brushTools;

        [SerializeField]
        int _minNumVoxelsPerAxis = 2;
        [SerializeField]
        int _maxNumVoxelsPerAxis = 14;
        int _numVoxelsPerAxis = 2;
        BrushType _brushType = BrushType.Sphere;
        Brush _brush;

        Stack<EditInfo> _undoStack;
        Stack<EditInfo> _redoStack;

        [SerializeField]
        ChunkSetting _chunkSetting;
        ChunkManager _chunkManager;

        void Awake()
        {
            _chunkManager = GetComponent<ChunkManager>();

            _brushTools = GetComponent<BrushTools>();
            _brush = _brushTools.BuildBrush(_numVoxelsPerAxis, _brushType);

            _undoStack = new Stack<EditInfo>();
            _redoStack = new Stack<EditInfo>();
        }

        void Update()
        {
            if (!IsActive)
            {
                return;
            }
            _text?.SetText(@"Current Mode: Edit
1: Switch View Mode
3: Switch Model Mode

BrushSize: " + _numVoxelsPerAxis + @"
BrushType: " + _brushType.ToString() + @"

mouse2: resize
s: switch sphere
c: switch cube
mouse0: add
mouse1: sub
u: undo
r: redo");

            ResizeBrush();
            SwitchBrushType();
            EditByBrush();
            UndoRedo();
        }

        void ResizeBrush()
        {
            var offset = Input.GetAxisRaw("Mouse ScrollWheel");
            if (offset > 0)
            {
                if (_numVoxelsPerAxis < _maxNumVoxelsPerAxis)
                {
                    ++_numVoxelsPerAxis;
                }
            }
            else if (offset < 0)
            {
                if (_minNumVoxelsPerAxis < _numVoxelsPerAxis)
                {
                    --_numVoxelsPerAxis;
                }
            }
        }

        void SwitchBrushType()
        {
            if (Input.GetKey(KeyCode.S))
            {
                _brushType = BrushType.Sphere;
            }
            if (Input.GetKey(KeyCode.C))
            {
                _brushType = BrushType.Cube;
            }
        }

        const float maxDistance = 1024;
        void EditByBrush()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, maxDistance))
                {
                    if ((_brush.NumVoxelsPerAxis != _numVoxelsPerAxis) || (_brush.BrushType != _brushType))
                    {
                        _brush = _brushTools.BuildBrush(_numVoxelsPerAxis, _brushType);
                    }
                    StartCoroutine(UseBrush(KeyCode.Mouse0, hit.point, 1.0f));
                }
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, maxDistance))
                {
                    if ((_brush.NumVoxelsPerAxis != _numVoxelsPerAxis) || (_brush.BrushType != _brushType))
                    {
                        _brush = _brushTools.BuildBrush(_numVoxelsPerAxis, _brushType);
                    }
                    StartCoroutine(UseBrush(KeyCode.Mouse1, hit.point, -1.0f));
                }
            }
        }

        IEnumerator UseBrush(KeyCode inputKey, Vector3 hitPoint, float weight)
        {
            while (Input.GetKey(inputKey))
            {
                EditInfo editInfo = new EditInfo();
                editInfo.BrushType = _brush.BrushType;
                editInfo.NumVoxelsPerAxis = _brush.NumVoxelsPerAxis;
                editInfo.EditPoint = hitPoint;
                editInfo.Weight = weight;
                editInfo.Delta = Time.deltaTime;
                _undoStack.Push(editInfo);
                _redoStack.Clear();

                _chunkManager.UseBrush(_brush, editInfo.EditPoint, editInfo.Weight, editInfo.Delta); 
                yield return null;
            }
        }

        void UndoRedo()
        {
            if (Input.GetKey(KeyCode.U))
            {
                if (_undoStack.Count > 0)
                {
                    EditInfo editInfo = _undoStack.Pop();
                    if ((_brush.NumVoxelsPerAxis != editInfo.NumVoxelsPerAxis) || (_brush.BrushType != editInfo.BrushType))
                    {
                        _brush = _brushTools.BuildBrush(editInfo.NumVoxelsPerAxis, editInfo.BrushType);
                    }
                    _chunkManager.UseBrush(_brush, editInfo.EditPoint, -editInfo.Weight, editInfo.Delta);
                    _redoStack.Push(editInfo);
                }
            }
            if (Input.GetKey(KeyCode.R))
            {
                if (_redoStack.Count > 0)
                {
                    EditInfo editInfo = _redoStack.Pop();
                    if ((_brush.NumVoxelsPerAxis != editInfo.NumVoxelsPerAxis) || (_brush.BrushType != editInfo.BrushType))
                    {
                        _brush = _brushTools.BuildBrush(editInfo.NumVoxelsPerAxis, editInfo.BrushType);
                    }
                    _chunkManager.UseBrush(_brush, editInfo.EditPoint, editInfo.Weight, editInfo.Delta);
                    _undoStack.Push(editInfo);
                }
            }
        }

    }

}
