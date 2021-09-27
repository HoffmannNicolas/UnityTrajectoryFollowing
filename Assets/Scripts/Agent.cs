using System; // For Action (Func that returns void)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Agent : MonoBehaviour {

    public float validDistance = 3f;
    public float angleRange = 90.0f;

    private GameObjectPooler sphereObjectPooler;
    private GameObjectPooler cylinderObjectPooler;
    private List<GameObject> sphereDrawing;
    private List<GameObject> cylinderDrawing;

    public GameObject spherePrefab;
    public GameObject cylinderPrefab;
    public TextMeshProUGUI StateDisplay;


    private float groundSize = 5;

    private List<GameObject> trajectory = new List<GameObject>();
    private Vector3 forceDirection;
    private float forceAmplitude;
    public float kp = 1f;

    private void deleteTrajectory() {
        while (trajectory.Count > 0) {
            Destroy(trajectory[0]);
            trajectory.RemoveAt(0);
        }
    }

    private void generateTrajectory() {
        deleteTrajectory();
        float angleBiais = 0;
        Vector3 onlinePosition = transform.position;
        // Vector3 onlineForward = GetComponent<Rigidbody>().velocity.normalized;
        // Vector3 onlineForward = ( - transform.position).normalized;
        Vector3 onlineForward = (new Vector3(UnityEngine.Random.Range(-1, 1), 0.0f, UnityEngine.Random.Range(-1, 1)) - transform.position).normalized;
        onlineForward[1] = 0.0f;

        for (int i = 0; i < 100; i++) {
            float deltaAngle = UnityEngine.Random.Range(-angleRange, angleRange) + angleRange * Mathf.Cos(i/3);
            float dist = UnityEngine.Random.Range(1.0f, 2.0f);
            onlineForward = Quaternion.Euler(0.0f, deltaAngle, 0.0f) * onlineForward;
            onlinePosition += onlineForward * dist;
            onlinePosition[1] = 0.5f;
            if ((Mathf.Abs(onlinePosition[0]) > (groundSize * 4.5f)) || (Mathf.Abs(onlinePosition[2]) > (groundSize * 4.5f)))
                break;
            GameObject nextPoint = new GameObject();
            nextPoint.name = "Target_" + i;
            nextPoint.transform.position = onlinePosition;
            // nextPoint.transform.parent = transform;
            trajectory.Add(nextPoint);
        }
    }

    void Start() {
            // For efficient drawing
        this.sphereObjectPooler = new GameObjectPooler(spherePrefab, "SpherePooler");
        this.cylinderObjectPooler = new GameObjectPooler(cylinderPrefab, "CylinderPooler");
        this.sphereDrawing = new List<GameObject>();
        this.cylinderDrawing = new List<GameObject>();

        generateTrajectory();
    }

    private void gotoPoint(Transform target) {
        Vector3 targetDirection = (target.position - transform.position).normalized;
        Vector3 forceToTarget = targetDirection * (0.3f + distanceTo(target) * kp);
        Vector3 forceAgainstInertia = (targetDirection - GetComponent<Rigidbody>().velocity.normalized) * GetComponent<Rigidbody>().velocity.magnitude;
        Vector3 totalForce = forceToTarget + forceAgainstInertia;
        this.forceDirection = totalForce.normalized;
        this.forceAmplitude = totalForce.magnitude;
        GetComponent<Rigidbody>().AddForce(this.forceDirection * this.forceAmplitude);
    }

    private float distanceTo(Transform target) {
        float dx = transform.position.x - target.position.x;
        float dy = transform.position.y - target.position.y;
        float dz = transform.position.z - target.position.z;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }


    void UpdateParams() {
          // Valid distance
        if (Input.GetKeyDown(KeyCode.O)) this.validDistance /= 1.1f;
        if (Input.GetKeyDown(KeyCode.P)) this.validDistance *= 1.1f;
          // Angle range
        if (Input.GetKeyDown(KeyCode.K)) this.angleRange /= 1.1f;
        if (Input.GetKeyDown(KeyCode.L)) this.angleRange *= 1.1f;
          // kp
        if (Input.GetKeyDown(KeyCode.U)) this.kp /= 1.1f;
        if (Input.GetKeyDown(KeyCode.I)) this.kp *= 1.1f;
          // Update display
        this.StateDisplay.text = "ValidDistance : " + this.validDistance + "\n"
          + "AngleRange : " + this.angleRange + "\n"
          + "Kp : " + this.kp;

    }

    void Update() {
        UpdateParams();
        if (trajectory.Count == 0) {
            generateTrajectory();
        }
        else {
            gotoPoint(trajectory[0].transform);
            if (distanceTo(trajectory[0].transform) < validDistance) {
                Destroy(trajectory[0]);
                trajectory.RemoveAt(0);
            }
        }
            // Free previous drawing s ressources
        while(this.sphereDrawing.Count > 0) {
            this.sphereObjectPooler.free(this.sphereDrawing[0]);
            this.sphereDrawing.RemoveAt(0);
        }
        while(this.cylinderDrawing.Count > 0) {
            this.cylinderObjectPooler.free(this.cylinderDrawing[0]);
            this.cylinderDrawing.RemoveAt(0);
        }
        draw(drawSphere_build, drawLine_build);
    }



    void OnDrawGizmos() {
        draw(drawSphere_gizmo, drawLine_gizmo);
    }

    void draw(Action<Vector3, Color, float> drawSphere, Action<Vector3, Vector3, Color> drawLine) {
            // Do not try to draw <trajectory> if it is not defined yet
        Vector3 previousPosition = transform.position;
        for (int i = 0; i < trajectory.Count; i++) {
            Vector3 nextPosition = trajectory[i].transform.position;
            drawSphere(nextPosition, Color.yellow, 0.3f);
            if (i == 0) {
                drawLine(previousPosition, previousPosition + this.forceDirection * this.forceAmplitude, Color.green);
            }
            else {
                drawLine(previousPosition, nextPosition, Color.white);
            }
            previousPosition = nextPosition;
        }
    }

    private void drawSphere_gizmo(Vector3 position, Color color, float radius) {
        Gizmos.color = color;
        Gizmos.DrawSphere(position, radius);
    }
    private void drawSphere_build(Vector3 position, Color color, float radius) {
        radius *= 2;
        GameObject sphere = this.sphereObjectPooler.get();
        this.sphereDrawing.Add(sphere);
        sphere.transform.position = position;
        sphere.GetComponent<Renderer>().material.SetColor("_Color", color);
        sphere.transform.localScale = new Vector3(radius, radius, radius);
    }
    private void drawLine_gizmo(Vector3 p1, Vector3 p2, Color color) {
        Gizmos.color = color;
        Gizmos.DrawLine(p1, p2);
    }
    private void drawLine_build(Vector3 p1, Vector3 p2, Color color) {
        if (color == Color.green)
            drawLine_build(p1, p2, color, 0.05f);
        else
            drawLine_build(p1, p2, color, 0.02f);
    }
    private void drawLine_build(Vector3 p1, Vector3 p2, Color color, float radius) {
        GameObject cylinder = this.cylinderObjectPooler.get();
        this.cylinderDrawing.Add(cylinder);
        cylinder.transform.position = (p1 + p2) / 2.0f;
        cylinder.GetComponent<Renderer>().material.color = color;
        float dx = p1[0] - p2[0];
        float dy = p1[2] - p2[2];
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        cylinder.transform.LookAt(p2);
        cylinder.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);
        cylinder.transform.localScale = new Vector3(radius, dist/2, radius);
    }
}
