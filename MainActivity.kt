package com.stem.arcontroller.presentation

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.os.VibrationEffect
import android.os.Vibrator
import android.view.WindowManager
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.text.BasicText
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.input.rotary.onRotaryScrollEvent
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.sp
import com.google.android.gms.wearable.MessageClient
import com.google.android.gms.wearable.Wearable

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Giu man hinh sang khi app dang mo
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        setContent { WatchApp() }
    }
}

// Gui tin nhan sang dien thoai qua Data Layer
fun sendToPhone(context: Context, path: String, text: String, onResult: (String) -> Unit) {
    Wearable.getNodeClient(context).connectedNodes
        .addOnSuccessListener { nodes ->
            if (nodes.isEmpty()) {
                onResult("Phone not found")
            } else {
                for (n in nodes) {
                    Wearable.getMessageClient(context).sendMessage(n.id, path, text.toByteArray())
                }
                onResult("Sent: $text")
            }
        }
        .addOnFailureListener { onResult("Send failed") }
}

// Rung theo mau (pattern: nghi, rung, nghi, rung, ...)
fun buzz(context: Context, pattern: LongArray) {
    val v = context.getSystemService(Vibrator::class.java) ?: return
    v.vibrate(VibrationEffect.createWaveform(pattern, -1))
}

val BUZZ_FIRE = longArrayOf(0, 40)                    // 1 nhip rat ngan
val BUZZ_ANGLE = longArrayOf(0, 150)                  // 1 nhip dai
val BUZZ_FORCE = longArrayOf(0, 50, 80, 50)           // 2 nhip ngan
val BUZZ_HIT = longArrayOf(0, 70, 60, 70, 60, 250)    // 3 nhip, nhip cuoi dai
val BUZZ_CALM = longArrayOf(0, 400, 300, 400, 300, 400)  // 3 nhip dai, cham: tran an
const val HR_ALERT = 65                                 // nguong nhip tim (bpm), chinh theo y

// Quyen doc nhip tim: Wear OS 6 (API 36+) dung quyen health moi, cu hon dung BODY_SENSORS
val HR_PERMISSION: String =
    if (Build.VERSION.SDK_INT >= 36) "android.permission.health.READ_HEART_RATE"
    else Manifest.permission.BODY_SENSORS

@Composable
fun WatchApp() {
    val context = LocalContext.current
    var mode by remember { mutableStateOf("ANGLE") }
    var status by remember { mutableStateOf("Ready") }
    var acc by remember { mutableStateOf(0f) }
    var bpm by remember { mutableStateOf(0) }
    var hrStatus by remember { mutableStateOf("starting") }
    var lastCalm by remember { mutableStateOf(0L) }
    val focusRequester = remember { FocusRequester() }

    // Doc nhip tim, moi mau gui sang dien thoai qua /game/hr
    val hrReader = remember {
        HeartRateReader(
            context,
            onBpm = { v ->
                bpm = v
                sendToPhone(context, "/game/hr", v.toString()) { }
                val now = System.currentTimeMillis()
                if (v >= HR_ALERT && now - lastCalm > 30000L) {
                    lastCalm = now
                    buzz(context, BUZZ_CALM)
                    status = "HR high: calm"
                }
            },
            onStatus = { s -> hrStatus = s }
        )
    }

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) hrReader.start() else hrStatus = "NO PERMISSION"
    }

    LaunchedEffect(Unit) {
        if (context.checkSelfPermission(HR_PERMISSION) == PackageManager.PERMISSION_GRANTED) {
            hrReader.start()
        } else {
            permissionLauncher.launch(HR_PERMISSION)
        }
    }

    // Nhan tin tu dien thoai: trung bia -> rung
    DisposableEffect(Unit) {
        val client = Wearable.getMessageClient(context)
        val listener = MessageClient.OnMessageReceivedListener { e ->
            if (e.path == "/game/vibrate") {
                buzz(context, BUZZ_HIT)
                status = "HIT!"
            }
        }
        client.addListener(listener)
        onDispose { client.removeListener(listener) }
    }

    // Xin focus de nhan su kien bezel, va bao che do hien tai cho dien thoai
    LaunchedEffect(Unit) {
        focusRequester.requestFocus()
        sendToPhone(context, "/game/mode", mode) { }
    }

    val modeColor = if (mode == "ANGLE") Color(0xFF4DD0E1) else Color(0xFFFFB74D)

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black)
            // Xoay bezel: moi 30 px = 1 nac
            .onRotaryScrollEvent {
                acc += it.verticalScrollPixels
                if (acc >= 30f) {
                    acc = 0f
                    sendToPhone(context, "/game/rotary", "$mode:1") { s -> status = s }
                } else if (acc <= -30f) {
                    acc = 0f
                    sendToPhone(context, "/game/rotary", "$mode:-1") { s -> status = s }
                }
                true
            }
            .focusRequester(focusRequester)
            .focusable()
            // Cham = FIRE, nhan giu = doi che do
            .pointerInput(Unit) {
                detectTapGestures(
                    onTap = {
                        buzz(context, BUZZ_FIRE)
                        sendToPhone(context, "/game/touch", "FIRE") { s -> status = s }
                    },
                    onLongPress = {
                        mode = if (mode == "ANGLE") "FORCE" else "ANGLE"
                        buzz(context, if (mode == "ANGLE") BUZZ_ANGLE else BUZZ_FORCE)
                        sendToPhone(context, "/game/mode", mode) { }
                        status = "Dial: $mode"
                    }
                )
            },
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        BasicText(
            text = mode,
            style = TextStyle(
                color = modeColor, fontSize = 34.sp,
                fontWeight = FontWeight.Bold, textAlign = TextAlign.Center
            )
        )
        BasicText(
            text = "Tap = FIRE",
            style = TextStyle(color = Color.White, fontSize = 18.sp, textAlign = TextAlign.Center)
        )
        BasicText(
            text = "Hold = switch dial",
            style = TextStyle(color = Color.Gray, fontSize = 14.sp, textAlign = TextAlign.Center)
        )
        BasicText(
            text = if (bpm > 0) "HR $bpm" else "HR -- ($hrStatus)",
            style = TextStyle(
                color = Color(0xFFEF5350), fontSize = 20.sp,
                fontWeight = FontWeight.Bold, textAlign = TextAlign.Center
            )
        )
        BasicText(
            text = status,
            style = TextStyle(color = Color.DarkGray, fontSize = 12.sp, textAlign = TextAlign.Center)
        )
    }
}
// END OF FILE