using UnityEngine;
using WaterWaveSurface;
internal class Disturbance : MonoBehaviour
{
    private Vector3? m_previous_left_mouse_position;

    [SerializeField]
    private float m_disturbance;

    [SerializeField]
    private WaterSurface m_waterSurface;

    [SerializeField]
    private bool m_enableRain = true;

    // Update is called once per frame
    void Update()
    {
        BoatPath();
        Rain();
    }

    private float Angle(Vector3 v1, Vector3 v2)
    {
        var angle = Vector3.SignedAngle(v1, v2, Vector3.up);
        if (angle < 0)
        {
            angle = 360 + angle;
        }
        return angle;
    }

    private void BoatPath()
    {

        if (Input.GetMouseButton(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float t = -ray.origin.y / ray.direction.y;
            var pos = ray.origin + t * ray.direction;
            if (m_previous_left_mouse_position != null)
            {
                float velocity = Vector3.Distance(m_previous_left_mouse_position.Value, pos) * Time.deltaTime;
                var direction = (pos - m_previous_left_mouse_position).Value.normalized;

                var angle1 = Angle(direction, Vector3.forward + Vector3.right * 0.5f);
                var angle2 = Angle(direction, Vector3.back + Vector3.right * 0.5f);
                m_waterSurface.AddPointDirectionDisturbance(new Vector3(pos.x, pos.z, angle1 * Mathf.Deg2Rad), m_disturbance * velocity);
                m_waterSurface.AddPointDirectionDisturbance(new Vector3(pos.x, pos.z, angle2 * Mathf.Deg2Rad), m_disturbance * velocity);
            }

            m_previous_left_mouse_position = pos;
        }
        else
        {
            m_previous_left_mouse_position = null;
        }
    }

    private void Rain()
    {
        if (m_enableRain)
        {
            var point = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50)); 
            m_waterSurface.AddPointDisturbance(point, m_disturbance);
        }
    }
}