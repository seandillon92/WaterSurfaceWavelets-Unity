using UnityEngine;
using WaterWaveSurface;
using static UnityEditorInternal.VersionControl.ListControl;

public class Boat : MonoBehaviour
{
    [SerializeField]
    private WaterSurface surface;

    private Vector3? m_previous_left_mouse_position;
    private Vector3? m_previous_right_mouse_position;

    [SerializeField]
    private Paddle leftPaddle;

    [SerializeField]
    private Paddle rightPaddle;

    [SerializeField]
    private float m_row_amplitude_splash = 0.05f;

    [SerializeField]
    private float m_row_amplitude = 0.1f;

    [SerializeField]
    private float m_row_speed_mutliplier_vertical = 0.01f;

    [SerializeField]
    private float m_row_speed_mutliplier_horizontal = 0.01f;

    [SerializeField]
    private float m_max_row_speed = 1.0f;

    [SerializeField]
    private float m_min_row_speed = 0.1f;

    private void Start()
    {
        row_param = Animator.StringToHash("row");
        leftPaddle.StartRow = OnRowLeft;
        rightPaddle.StartRow = OnRowRight;
    }

    // Update is called once per frame
    void Update()
    {
        Render();
        Controls();
        MoveBoat();


        if (m_rowing_left)
        {
            MakeWavesLeft();
        }

        if (m_rowing_right)
        {
            MakeWavesRight();
        }
    }

    private int row_param;
    private bool m_rowing_left;
    private bool m_rowing_right;
    private float m_boat_speed;
    private float m_boat_rotate_speed_left;
    private float m_boat_rotate_speed_right;


    private void OnRowLeft(Paddle.RowEventType e)
    {
        switch (e)
        {
            case Paddle.RowEventType.Start:
                m_rowing_left = true;
                var pos2 = new Vector2(leftPaddle.end.position.x, leftPaddle.end.position.z);
                surface.AddPointDisturbance(pos2, m_row_amplitude_splash);
                break;
            case Paddle.RowEventType.End:
                m_rowing_left = false;
                break;
        }
    }

    private void MoveBoat()
    {
        m_boat_speed *= 0.99f;
        m_boat_rotate_speed_right *= 0.99f;
        m_boat_rotate_speed_left *= 0.99f;

        if (m_rowing_left)
        {
            var speed = leftPaddle.Animator.speed;
            m_boat_speed += 0.001f * speed;
            m_boat_rotate_speed_left += 0.005f * speed;

        }
        if (m_rowing_right)
        {
            var speed = rightPaddle.Animator.speed;
            m_boat_speed += 0.001f * speed;
            m_boat_rotate_speed_right += 0.005f * speed;
        }

        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        transform.Rotate(Vector3.up * -m_boat_rotate_speed_left);
        transform.Rotate(Vector3.up * m_boat_rotate_speed_right);
        transform.position += transform.forward * m_boat_speed;
    }

    private void MakeWavesLeft()
    {
        var forward = leftPaddle.end.forward;
        forward.y = 0;

        var angle = -Vector3.SignedAngle(Vector3.right, forward.normalized, Vector3.up);
        var pos3 = new Vector3(leftPaddle.end.position.x, leftPaddle.end.position.z, angle);
        var paddleSpeed = leftPaddle.Animator.speed;
        surface.AddPointDirectionDisturbance(pos3, m_row_amplitude * paddleSpeed);
    }

    private void OnRowRight(Paddle.RowEventType e)
    {
        switch (e)
        {
            case Paddle.RowEventType.Start:
                m_rowing_right = true;
                var pos2 = new Vector2(rightPaddle.end.position.x, rightPaddle.end.position.z);
                surface.AddPointDisturbance(pos2, m_row_amplitude_splash);
                break;
            case Paddle.RowEventType.End:
                m_rowing_right = false;
                break;
        }
    }

    private void MakeWavesRight()
    {
        var forward = rightPaddle.end.forward;
        forward.y = 0;

        var angle = -Vector3.SignedAngle(Vector3.right, forward.normalized, Vector3.up);
        var pos3 = new Vector3(rightPaddle.end.position.x, rightPaddle.end.position.z, angle);
        var paddleSpeed = rightPaddle.Animator.speed;
        surface.AddPointDirectionDisturbance(pos3, m_row_amplitude * paddleSpeed);
    }

    private void Render()
    {
        var mat = surface.Settings.visualization.material;
        mat.SetMatrix("_BoatTransform", transform.worldToLocalMatrix);
    }

    private bool ShouldRow(Paddle paddle, int mouseBtn, ref Vector3? prevMousePos)
    {
        var row = false;
        
        paddle.Animator.speed = 1.0f;
        var state = paddle.Animator.GetCurrentAnimatorStateInfo(0);
        var isRow = state.IsName("Row");

        if (Input.GetMouseButton(mouseBtn))
        {
            if (isRow)
            {
                row = true;
            }

            if (prevMousePos != null && isRow)
            {
                var delta = Input.mousePosition - prevMousePos;

                var dir = delta.Value.normalized;
                paddle.Animator.speed = 0f;

                if (Vector3.Angle(Vector3.down, dir) < 45f)
                {
                        paddle.Animator.speed = Mathf.Clamp(
                            -delta.Value.y * 
                            m_row_speed_mutliplier_vertical,
                            m_min_row_speed,
                            m_max_row_speed);
                }
            }
            else
            {
                if (state.IsName("Idle"))
                {
                    row = true;
                }
            }
            
            prevMousePos = Input.mousePosition;
        }
        else
        {
            prevMousePos = null;
        }
        return row;
    }

    private void Controls()
    {
 
        leftPaddle.Animator.SetBool(row_param, ShouldRow(leftPaddle, 0, ref m_previous_left_mouse_position));
        rightPaddle.Animator.SetBool(row_param, ShouldRow(rightPaddle, 1, ref m_previous_right_mouse_position));
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }
}