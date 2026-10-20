using UnityEngine;

public class WearReceiver : MonoBehaviour
{
    public CannonController cannon;
    public string lastMessage = "";
    public bool showDebug = false;   // bat de hien dong "Watch: ..."
    int lastScore = 0;
        public static int HeartRate = 0;           // nhip tim moi nhat (bpm)
    public static int HeartRateCount = 0;      // so mau da nhan
    public static float HeartRateTime = -100f; // thoi diem nhan mau cuoi

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var bridge = new AndroidJavaClass("com.stem.arcontroller.WearBridge"))
            {
                bridge.CallStatic("start", activity, gameObject.name, "OnWearMessage");
            }
        }
        catch (System.Exception e) { Debug.LogError("WearBridge: " + e); }
#endif
    }

    void Update()
    {
        int s = (int)Target.Score;
        if (s > lastScore) { SendToWatch("/game/vibrate", "HIT"); }
        lastScore = s;
    }

    void SendToWatch(string path, string payload)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var bridge = new AndroidJavaClass("com.stem.arcontroller.WearBridge"))
            {
                bridge.CallStatic("sendToWatch", activity, path, payload);
            }
        }
        catch (System.Exception e) { Debug.LogError("SendToWatch: " + e); }
#endif
    }

    // Duoc goi tu Java qua UnitySendMessage. msg = path|payload
    public void OnWearMessage(string msg)
    {
        string[] parts = msg.Split('|');
        string path = parts[0];
        string payload = parts.Length > 1 ? parts[1] : "";

        if (path == "/game/ping") { SendToWatch("/game/pong2", payload); return; }
                if (path == "/game/hr")
        {
            int bpm;
            if (int.TryParse(payload, out bpm))
            {
                HeartRate = bpm;
                HeartRateCount++;
                HeartRateTime = Time.time;
            }
            return;
        }

        lastMessage = msg;
        if (cannon == null) return;

        if (path == "/game/mode") { cannon.dialMode = payload; }
        else if (path == "/game/touch") { if (payload == "FIRE") cannon.Fire(); }
        else if (path == "/game/rotary")
        {
            string[] p = payload.Split(':');
            int dir;
            if (p.Length == 2 && int.TryParse(p[1], out dir))
            {
                if (p[0] == "ANGLE") cannon.AdjustAngle(5f * dir);
                else if (p[0] == "FORCE") cannon.AdjustForce(2f * dir);
            }
        }
    }

    void OnGUI()
    {
        if (!showDebug) return;
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 28 };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, Screen.height - 60, 1000, 50), "Watch: " + lastMessage, style);
    }
}