using UnityEngine;
using UnityEngine.UIElements;
using WaterWaveSurface;
internal class Disturbance : MonoBehaviour
{
    private Vector3? m_previous_distrurbance_mouse_position;

    [SerializeField]
    private float m_disturbance;

    [SerializeField]
    private WaterSurface m_waterSurface;

    [SerializeField]
    private bool m_enableRain = true;

    [SerializeField]
    private MouseButton m_disturbanceMouseButton;

    // Update is called once per frame
    void Update()
    {
        BoatPath();
        Rain();
    }

    private void BoatPath()
    {

        if (Input.GetMouseButton((int)m_disturbanceMouseButton))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var waterLevel = m_waterSurface.Settings.environment.water_level;
            var posY = ray.origin.y - waterLevel;
            float t = -posY / ray.direction.y;
            var pos = ray.origin + t * ray.direction;

            if (m_previous_distrurbance_mouse_position != null)
            {
                float velocity = Vector3.Distance(m_previous_distrurbance_mouse_position.Value, pos) * Time.deltaTime;
                var direction = (pos - m_previous_distrurbance_mouse_position).Value.normalized;
                m_waterSurface.AddPointDirectionDisturbance(pos, direction, m_disturbance, true);
            }

            m_previous_distrurbance_mouse_position = pos;
        }
        else
        {
            m_previous_distrurbance_mouse_position = null;
        }
    }

    private void Rain()
    {
        if (!m_enableRain)
            return;

        var center = m_waterSurface.Settings.environment.transform.GetPosition();
        var size = m_waterSurface.Settings.environment.size;

        var point = new Vector2(0,0);

        point.x += Random.Range((float)-size.x, (float)size.x);
        point.y += Random.Range((float)-size.y, (float)size.y);

        var localPoint = new Vector3(point.x, 0, point.y);
        var worldPoint = m_waterSurface.Settings.environment.transform.rotation * localPoint;
        point.x = worldPoint.x;
        point.y = worldPoint.z;

        point.x += center.x;
        point.y += center.z;

        m_waterSurface.AddPointDisturbance(point, m_disturbance);
    }
}
