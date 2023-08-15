using UnityEngine;
using WaterWaveSurface;

public class Boat : MonoBehaviour
{
    [SerializeField]
    private WaterSurface surface;

    private Vector3? m_previous_left_mouse_position;

    [SerializeField]
    private Paddle leftPaddle;

    [SerializeField]
    private Paddle rightPaddle;

    private void Start()
    {
        row_param = Animator.StringToHash("row");
        leftPaddle.StartRow = OnRowLeft;
        rightPaddle.StartRow= OnRowRight;
    }

    // Update is called once per frame
    void Update()
    {
        Render();
        Controls();
    }

    private int row_param;

    private void OnRowLeft()
    {
        var angle = -Vector3.Angle(Vector3.left, leftPaddle.end.forward);
        var pos = new Vector3(leftPaddle.end.position.x, leftPaddle.end.position.z, angle);
        surface.AddPointDirectionDisturbance(pos, 1);
    }

    private void OnRowRight()
    {
        var angle = -Vector3.Angle(Vector3.left, rightPaddle.end.forward);
        var pos = new Vector3(rightPaddle.end.position.x, rightPaddle.end.position.z, angle);
        surface.AddPointDirectionDisturbance(pos, 1);
    }

    private void Render()
    {
        var mat = surface.Settings.visualization.material;
        mat.SetMatrix("_BoatTransform", transform.worldToLocalMatrix);
    }

    private void Controls()
    {
        var left = false;
        var right = false;
        if (Input.GetMouseButton(0))
        {
            if (m_previous_left_mouse_position != null)
            {
                var delta = m_previous_left_mouse_position - Input.mousePosition;
  
                if (delta.Value.magnitude > 0.4)
                {
                    var dir = delta.Value.normalized;

                    if (Vector3.Angle(Vector3.up, dir) < 45f)
                    {
                        left = true;
                        right = true;
                    }
                    else if (Vector3.Angle(Vector3.down, dir) < 45f)
                    {
                        Debug.Log("Both Oards down");
                    }
                    else if (Vector3.Angle(Vector3.right, dir) < 45f)
                    {
                        left = true;
                        right = false;
                    }
                    else
                    {
                        left = false;
                        right = true;
                    }
                }
            }
            m_previous_left_mouse_position = Input.mousePosition;
        }
        else
        {
            m_previous_left_mouse_position = null;
        }
        leftPaddle.Animator.SetBool(row_param, left);
        rightPaddle.Animator.SetBool(row_param, right);
    }
}
