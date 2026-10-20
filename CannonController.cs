using UnityEngine;

public class CannonController : MonoBehaviour
{
    public Transform muzzle;
    public GameObject projectilePrefab;
    public float angle = 45f;
    public float force = 20f;
    public float minAngle = 10f, maxAngle = 80f;
    public float minForce = 10f, maxForce = 30f;

    // Che do bezel dang dieu khien: "ANGLE" hoac "FORCE" (WearReceiver gan)
    public string dialMode = "ANGLE";

    // Tuy chon hien thi
    public bool darkStyle = true;       // nen den + mau sang cho kinh AR
    public bool showTrajectory = true;  // duong bay du doan
    public float cannonScale = 1.8f;    // phong to phao
    public float targetScale = 1.8f;    // phong to bia
    public int hrAlert = 65;           // nguong nhip tim cao (bpm), khop voi HR_ALERT tren dong ho

    static readonly Color ColAngle = new Color(0.30f, 0.82f, 0.88f);
    static readonly Color ColForce = new Color(1.00f, 0.72f, 0.30f);

    void Start()
    {
        Apply();
        if (darkStyle) StyleScene();
        ScaleObjects();
    }

    void ScaleObjects()
    {
        transform.localScale *= cannonScale;
        GameObject t = GameObject.Find("Target");
        if (t != null) t.transform.localScale *= targetScale;
    }

    void StyleScene()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }
        SetColor(GameObject.Find("Ground"), new Color(0.08f, 0.10f, 0.14f), false);
        SetColor(GameObject.Find("Target"), new Color(1.0f, 0.25f, 0.20f), false);
        SetColor(gameObject, new Color(0.30f, 0.80f, 0.90f), true);
    }

    void SetColor(GameObject go, Color c, bool includeChildren)
    {
        if (go == null) return;
        Renderer[] rs = includeChildren ? go.GetComponentsInChildren<Renderer>()
                                        : go.GetComponents<Renderer>();
        foreach (Renderer r in rs) r.material.color = c;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))    AdjustAngle(30f * Time.deltaTime);
        if (Input.GetKey(KeyCode.DownArrow))  AdjustAngle(-30f * Time.deltaTime);
        if (Input.GetKey(KeyCode.RightArrow)) AdjustForce(10f * Time.deltaTime);
        if (Input.GetKey(KeyCode.LeftArrow))  AdjustForce(-10f * Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Space))  Fire();
    }

    public void AdjustAngle(float delta)
    {
        angle = Mathf.Clamp(angle + delta, minAngle, maxAngle);
        Apply();
    }

    public void AdjustForce(float delta)
    {
        force = Mathf.Clamp(force + delta, minForce, maxForce);
    }

    void Apply()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void Fire()
    {
        GameObject go = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        go.GetComponent<Rigidbody>().velocity = muzzle.right * force;
        if (darkStyle)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.9f, 0.2f);
        }
    }

    // Duong bay ly tuong (khong co luc can): p = p0 + v0*t + 0.5*g*t^2
    void DrawTrajectory()
    {
        Camera cam = Camera.main;
        if (muzzle == null || cam == null) return;
        Vector3 p0 = muzzle.position;
        Vector3 v0 = muzzle.right * force;
        Vector3 g = Physics.gravity;
        float size = Mathf.Max(3f, Screen.height * 0.005f);
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        for (float t = 0.1f; t < 6f; t += 0.12f)
        {
            Vector3 p = p0 + v0 * t + 0.5f * g * t * t;
            if (p.y < 0f) break;
            Vector3 s = cam.WorldToScreenPoint(p);
            if (s.z < 0f) break;
            GUI.DrawTexture(new Rect(s.x - size / 2f, Screen.height - s.y - size / 2f, size, size),
                            Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    void HudLine(float y, string label, string value, bool active, Color c, float k)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.alignment = TextAnchor.MiddleCenter;
        st.fontSize = Mathf.RoundToInt((active ? 64f : 40f) * k);
        st.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
        st.normal.textColor = active ? c : new Color(0.55f, 0.55f, 0.55f);
        string txt = label + "  " + value;
        if (active) txt = "<  " + txt + "  >";
        GUI.Label(new Rect(0f, y, Screen.width, 80f * k), txt, st);
    }

    void OnGUI()
    {
        float k = Screen.height / 1080f;
        if (showTrajectory) DrawTrajectory();

        bool a = dialMode == "ANGLE";
        HudLine(15f * k, "ANGLE", angle.ToString("0") + "\u00B0", a, ColAngle, k);
        HudLine(100f * k, "FORCE", force.ToString("0"), !a, ColForce, k);

        GUIStyle sc = new GUIStyle(GUI.skin.label);
        sc.alignment = TextAnchor.MiddleCenter;
        sc.fontSize = Mathf.RoundToInt(40f * k);
        sc.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, 185f * k, Screen.width, 60f * k), "Score: " + Target.Score, sc);

        // Nhip tim tu dong ho: mau theo vung, hien CALM DOWN khi vuot nguong
        bool hrFresh = WearReceiver.HeartRate > 0 && Time.time - WearReceiver.HeartRateTime < 5f;
        bool hrHigh = hrFresh && WearReceiver.HeartRate >= hrAlert;
        Color hrColor = new Color(0.40f, 0.85f, 0.45f);                               // xanh la: binh thuong
        if (hrFresh && WearReceiver.HeartRate >= hrAlert - 20) hrColor = new Color(1.0f, 0.80f, 0.20f); // vang: gan nguong
        if (hrHigh) hrColor = new Color(0.94f, 0.33f, 0.31f);                         // do: cao
        if (!hrFresh) hrColor = new Color(0.55f, 0.55f, 0.55f);                       // xam: chua co du lieu
        GUIStyle hr = new GUIStyle(GUI.skin.label);
        hr.alignment = TextAnchor.MiddleCenter;
        hr.fontSize = Mathf.RoundToInt(40f * k);
        hr.fontStyle = hrHigh ? FontStyle.Bold : FontStyle.Normal;
        hr.normal.textColor = hrColor;
        GUI.Label(new Rect(0f, 245f * k, Screen.width, 60f * k),
                  "HR  " + (hrFresh ? WearReceiver.HeartRate.ToString() : "--") + (hrHigh ? "   CALM DOWN" : ""), hr);

    }
}