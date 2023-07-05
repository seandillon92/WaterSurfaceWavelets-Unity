using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private Camera m_Camera;

    private Vector3? m_last_right_mouse_pos;

    private void LateUpdate()
    {

            HandleRightMouse();
    }

    private void HandleRightMouse()
    {
       
        if (Input.GetMouseButton(1))
        {
            var pos = Input.mousePosition;
            if (m_last_right_mouse_pos != null)
            {
                var velocity =  pos - m_last_right_mouse_pos;
                velocity *= Time.deltaTime;
                m_Camera.transform.rotation =
                        Quaternion.AngleAxis(velocity.Value.x, Vector3.up) *
                        m_Camera.transform.rotation *
                        Quaternion.AngleAxis(velocity.Value.y, Vector3.right);
            }
            m_last_right_mouse_pos = pos;
        }
        else
        {
            m_last_right_mouse_pos = null;
        }
    }
}
