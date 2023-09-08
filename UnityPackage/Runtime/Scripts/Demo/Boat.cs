using UnityEngine;
using WaterWaveSurface;

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
    private Transform front;

    [SerializeField]
    private float m_row_amplitude_splash = 0.05f;

    [SerializeField]
    private float m_row_amplitude = 0.1f;

    [SerializeField]
    private float m_front_amplitude = 2f;

    [SerializeField]
    private float m_row_speed_mutliplier_vertical = 0.01f;

    [SerializeField]
    private float m_row_speed_mutliplier_horizontal = 0.01f;

    [SerializeField]
    private float m_row_acceleration = 0.01f;

    [SerializeField]
    private float m_row_rotate_acceleration = 0.01f;

    [SerializeField]
    private float m_max_row_speed = 1.0f;

    [SerializeField]
    private float m_min_row_speed = 0.1f;

    [SerializeField]
    private bool m_make_waves;

    [SerializeField]
    private int m_row_particles_min = 1000;

    [SerializeField]
    private int m_row_particles_max = 20000;

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

        if (m_make_waves)
        {
            MakeWaves();
        }
    }

    private void MakeWaves()
    {
        if (m_rowing_left)
        {
            MakeWavesLeft();
        }

        if (m_rowing_right)
        {
            MakeWavesRight();
        }

        MakeWavesFront();
    }

    private int row_param;
    private bool m_rowing_left;
    private bool m_rowing_right;
    private float m_boat_speed;
    private float m_boat_rotate_speed_left;
    private float m_boat_rotate_speed_right;


    private int GetParticlesNum(Paddle paddle)
    {
        var t = (paddle.Animator.speed - m_min_row_speed) / (m_max_row_speed - m_min_row_speed);
        return (int)Mathf.Lerp(m_row_particles_min, m_row_particles_max, t);
    }

    private void OnRowLeft(Paddle.RowEventType e)
    {
        switch (e)
        {
            case Paddle.RowEventType.Start:
                m_rowing_left = true;
                var posxz = new Vector2(leftPaddle.end.position.x, leftPaddle.end.position.z);

                leftPaddle.particles.Emit(GetParticlesNum(leftPaddle));
                if (m_make_waves)
                {
                    surface.AddPointDisturbance(posxz, m_row_amplitude_splash);
                }
                break;
            case Paddle.RowEventType.End:
                m_rowing_left = false;
                leftPaddle.particles.Emit(GetParticlesNum(leftPaddle));
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
            m_boat_speed +=  speed * Time.deltaTime * m_row_acceleration;
            m_boat_rotate_speed_left += speed * Time.deltaTime * m_row_rotate_acceleration;

        }
        if (m_rowing_right)
        {
            var speed = rightPaddle.Animator.speed;
            m_boat_speed += speed * Time.deltaTime * m_row_acceleration;
            m_boat_rotate_speed_right += speed * Time.deltaTime * m_row_rotate_acceleration;
        }


        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        transform.Rotate(Vector3.up * -m_boat_rotate_speed_left);
        transform.Rotate(Vector3.up * m_boat_rotate_speed_right);
        transform.position += transform.forward * m_boat_speed;
    }

    private void MakeWavesLeft()
    {
        var dir = leftPaddle.end.forward;
        var pos = leftPaddle.end.position;
        var paddleSpeed = leftPaddle.Animator.speed;
        surface.AddPointDirectionDisturbance(pos, dir, m_row_amplitude * paddleSpeed, true);
    }

    private void MakeWavesFront()
    {
        var dir = front.forward;
        var pos = front.position;
        var speed = m_boat_speed;
        surface.AddPointDirectionDisturbance(pos, dir, speed * m_front_amplitude, true);
    }

    private void OnRowRight(Paddle.RowEventType e)
    {
        switch (e)
        {
            case Paddle.RowEventType.Start:
                m_rowing_right = true;
                var pos2 = new Vector2(rightPaddle.end.position.x, rightPaddle.end.position.z);
                rightPaddle.particles.Emit(GetParticlesNum(rightPaddle));
                if (m_make_waves)
                {
                    surface.AddPointDisturbance(pos2, m_row_amplitude_splash);
                }
                break;
            case Paddle.RowEventType.End:
                rightPaddle.particles.Emit(GetParticlesNum(rightPaddle));
                m_rowing_right = false;
                break;
        }
    }

    private void MakeWavesRight()
    {
        var dir = rightPaddle.end.forward;
        var pos = rightPaddle.end.position;
        var paddleSpeed = rightPaddle.Animator.speed;
        surface.AddPointDirectionDisturbance(pos, dir, m_row_amplitude * paddleSpeed, true);
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
}
