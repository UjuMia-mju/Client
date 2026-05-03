using System.Collections.Generic;
using UnityEngine;

public class ThrowTrajectoryPreview : MonoBehaviour
{
    [SerializeField] private int maxSegments = 48;
    [SerializeField] private float timeStep = 0.04f;
    [SerializeField] private LayerMask obstacleMask;

    private LineRenderer _lr;
    private PlanetGravity _planet;

    private const float GravityAccel = 10f;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        if (_lr == null)
            _lr = gameObject.AddComponent<LineRenderer>();

        _lr.useWorldSpace = true;
        _lr.textureMode = LineTextureMode.Stretch;
        _lr.numCornerVertices = 2;
        _lr.numCapVertices = 2;
        _lr.startWidth = 0.06f;
        _lr.endWidth = 0.03f;
        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows = false;

        var sh = Shader.Find("Sprites/Default");
        if (sh != null)
        {
            _lr.material = new Material(sh);
            _lr.startColor = new Color(1f, 0.92f, 0.2f, 0.9f);
            _lr.endColor = new Color(1f, 0.45f, 0f, 0.35f);
        }

        _lr.enabled = false;
        _lr.positionCount = 0;
    }

    private void Start()
    {
        _planet = FindFirstObjectByType<PlanetGravity>();
        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask(Define.Layer.GROUND, Define.Layer.WALL, Define.Layer.WALKABLE_COLLIDER);
    }

    public void ShowTrajectory(Vector3 origin, Vector3 initialVelocity, float mass)
    {
        if (_lr == null || _planet == null)
            return;

        mass = Mathf.Max(0.01f, mass);
        var points = new List<Vector3>(maxSegments) { origin };

        Vector3 pos = origin;
        Vector3 vel = initialVelocity;

        for (int i = 1; i < maxSegments; i++)
        {
            Vector3 inward = (_planet.transform.position - pos).normalized;
            Vector3 accel = inward * (GravityAccel / mass);
            vel += accel * timeStep;
            Vector3 next = pos + vel * timeStep;

            if (Physics.Linecast(pos, next, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                points.Add(hit.point);
                break;
            }

            pos = next;
            points.Add(pos);
        }

        _lr.positionCount = points.Count;
        _lr.SetPositions(points.ToArray());
        _lr.enabled = true;
    }

    public void Hide()
    {
        if (_lr == null) return;
        _lr.enabled = false;
        _lr.positionCount = 0;
    }
}
