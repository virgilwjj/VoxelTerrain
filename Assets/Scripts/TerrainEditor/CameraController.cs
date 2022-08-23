using TMPro;
using UnityEngine;

namespace VoxelTerrain
{
    public class CameraController : MonoBehaviour
    {
        public bool IsActive = false;
        [SerializeField]
        TextMeshProUGUI _text;

        Camera _camera;
        Transform _cameraTrans;

        [SerializeField]
        float _moveSpeed = 3.0f;
        [SerializeField]
        float _dragSensitivity = 1.0f;
        [SerializeField]
        float _rotateSensitivity = 3.0f;
        [SerializeField]
        float _fovSensitivity = 30.0f;

        bool _needUpdate;
        ChunkManager _chunkManager;

        void Awake()
        {
            _camera = Camera.main;
            _cameraTrans = _camera.transform;

            _chunkManager = GetComponent<ChunkManager>();
        }

        void Update()
        {
            if (!IsActive)
            {
                return;
            }
            _text?.SetText(@"Current Mode: View
2: Switch Edit Mode
3: Switch Model Mode

a: left
d: right
q: up
e: down
w: forward
s: back
mouse0: drag
mouse1: rotate
mouse2: zoom");

            _needUpdate = false;

            moveByKeyboard();
            dragByMouse();
            rotateByMouse();
            zoomByMouse();

            if (_needUpdate)
            {
                _chunkManager?.UpdateByCamera();
            }
        }

        void moveByKeyboard()
        {
            var direction = Vector3.zero;
            if (Input.GetKey(KeyCode.A))
            {
                direction -= Vector3.right;
                _needUpdate = true;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
                _needUpdate = true;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction -= Vector3.up;
                _needUpdate = true;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.up;
                _needUpdate = true;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction -= Vector3.forward;
                _needUpdate = true;
            }
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.forward;
                _needUpdate = true;
            }
            _cameraTrans.Translate(direction * _moveSpeed * Time.deltaTime);
        }

        void dragByMouse()
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Vector3 direction = Vector3.zero;
                direction -= Input.GetAxisRaw("Mouse X") * Vector3.right;
                direction -= Input.GetAxisRaw("Mouse Y") * Vector3.up;
                _cameraTrans.Translate(direction * _dragSensitivity);
                _needUpdate = true;
            }
        }

        void rotateByMouse()
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                Vector3 rotation = Vector3.zero;
                rotation.x = - Input.GetAxis("Mouse Y");
                rotation.y = Input.GetAxis("Mouse X");
                _cameraTrans.rotation = Quaternion.Euler(_cameraTrans.eulerAngles + rotation * _rotateSensitivity);
                _needUpdate = true;
            }
        }

        void zoomByMouse()
        {
            var offset = Input.GetAxis("Mouse ScrollWheel");
            if (offset != 0)
            {
                _camera.fieldOfView -= offset * _fovSensitivity;
            }
        }

    }

}
