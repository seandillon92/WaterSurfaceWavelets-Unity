using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private Camera m_Camera;

    [SerializeField]
    private float scrollSpeed = 3.0f;

    [SerializeField]
    private bool rotate;

    [SerializeField]
    private bool translate;

    private Vector3? m_last_right_mouse_pos;
    private Vector3? m_last_middle_mouse_pos;

    private void Update()
    {
        if (rotate)
        {
            HandleRightMouse();
        }

        if (translate)
        {
            HandleMiddleMouse();
            HandleMouseScroll();
        }
    }

    private void HandleMouseScroll()
    {
        if (Input.mouseScrollDelta.magnitude > 0)
        {
            m_Camera.transform.position += 
                m_Camera.transform.forward * 
                -Input.mouseScrollDelta.y * 
                scrollSpeed;
        }
    }

    private void HandleMiddleMouse()
    {
        if (Input.GetMouseButton(2))
        {
            var pos = Input.mousePosition;
            if (m_last_middle_mouse_pos != null)
            {
                var velocity = pos - m_last_middle_mouse_pos;
                velocity *= Time.deltaTime;
                m_Camera.transform.position =
                    m_Camera.transform.position + 
                    m_Camera.transform.up * -velocity.Value.y + 
                    m_Camera.transform.right * velocity.Value.x;
            }
            m_last_middle_mouse_pos = pos;
        }
        else
        {
            m_last_middle_mouse_pos = null;
        }
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
