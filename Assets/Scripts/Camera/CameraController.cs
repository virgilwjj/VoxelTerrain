using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        float _moveSpeed = 3.0f;
        [SerializeField]
        float _sensitivityDrag = 1.0f;
        [SerializeField]
        float _sensitivityRotate = 3.0f;
        [SerializeField]
        float _sensitivetyMouseWheel = 30.0f;

        Camera _camera;
        Transform _cameraTrans;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _cameraTrans = _camera.transform;
        }

        void Update()
        {
            KeyBoardTranslate();
            MouseLeftTranslate();
            MouseRightRotate();
            MouseWheelFoV();
        }

        void KeyBoardTranslate()
        {
            if (Input.GetKey(KeyCode.W))
            {
                _cameraTrans.Translate(Vector3.forward * Time.deltaTime * _moveSpeed);
            }
 
            if (Input.GetKey(KeyCode.S))
            {
                _cameraTrans.Translate(Vector3.back * Time.deltaTime * _moveSpeed);
            }
 
            if (Input.GetKey(KeyCode.A))
            {
                _cameraTrans.Translate(Vector3.left * Time.deltaTime * _moveSpeed);
            }
 
            if (Input.GetKey(KeyCode.D))
            {
                _cameraTrans.Translate(Vector3.right * Time.deltaTime * _moveSpeed);
            }
        }

        void MouseLeftTranslate()
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Vector3 position = _cameraTrans.position - _cameraTrans.right * Input.GetAxisRaw("Mouse X") * Time.timeScale * _sensitivityDrag;
                _cameraTrans.position = position - _cameraTrans.up * Input.GetAxisRaw("Mouse Y") * Time.timeScale * _sensitivityDrag;
            }
        }

        void MouseRightRotate()
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                Vector3 rotation = Vector3.zero;
                rotation.x = Input.GetAxis("Mouse Y") * _sensitivityRotate;
                rotation.y = Input.GetAxis("Mouse X") * _sensitivityRotate;
                _cameraTrans.rotation = Quaternion.Euler(_cameraTrans.eulerAngles + rotation);
            }
        }

        void MouseWheelFoV()
        {
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                _camera.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * _sensitivetyMouseWheel;
            }
        }

    }

}
