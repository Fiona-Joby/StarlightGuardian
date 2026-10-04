"""
Starlight Guardian – Webcam Hand Pose Detection Server (UDP)
=============================================================

Captures the webcam, runs MediaPipe Hand Landmarker, classifies the gesture,
and sends the result to Unity via UDP (localhost:8765).

Gesture mapping:
  • Right hand index finger raised (other fingers curled) → "right"  (run right)
  • Left  hand index finger raised (other fingers curled) → "left"   (run left)
  • Both hands open and raised                            → "reach"  (catch stars)
  • Anything else                                         → "idle"

Requirements:
  pip install mediapipe opencv-python

Usage:
  python pose_server.py          # sends to UDP localhost:8765
  python pose_server.py --port 9000
"""

import argparse
import json
import os
import socket
import time
import cv2
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision

# ────────────────────────────────────────────────────────────────────
#  Hand Landmark indices
# ────────────────────────────────────────────────────────────────────

WRIST = 0
THUMB_TIP = 4
THUMB_IP = 3
INDEX_TIP = 8
INDEX_PIP = 6
MIDDLE_TIP = 12
MIDDLE_PIP = 10
RING_TIP = 16
RING_PIP = 14
PINKY_TIP = 20
PINKY_PIP = 18

FINGER_TIPS = [THUMB_TIP, INDEX_TIP, MIDDLE_TIP, RING_TIP, PINKY_TIP]
FINGER_PIPS = [THUMB_IP, INDEX_PIP, MIDDLE_PIP, RING_PIP, PINKY_PIP]


def is_finger_raised(landmarks, finger_idx: int) -> bool:
    """A finger is raised when its TIP is above (lower y) its PIP joint."""
    tip = landmarks[FINGER_TIPS[finger_idx]]
    pip = landmarks[FINGER_PIPS[finger_idx]]
    return tip.y < pip.y


def count_raised_fingers(landmarks) -> int:
    return sum(1 for i in range(5) if is_finger_raised(landmarks, i))


def is_index_only(landmarks) -> bool:
    """True when ONLY the index finger is raised (others curled)."""
    index_up = is_finger_raised(landmarks, 1)
    others_down = all(not is_finger_raised(landmarks, i) for i in [2, 3, 4])
    return index_up and others_down


def is_hand_open(landmarks) -> bool:
    """True when at least 4 fingers are raised."""
    return count_raised_fingers(landmarks) >= 4


def is_hand_raised(landmarks) -> bool:
    """True when the wrist is above the midpoint of the frame."""
    return landmarks[WRIST].y < 0.6


# ────────────────────────────────────────────────────────────────────
#  Gesture classification
# ────────────────────────────────────────────────────────────────────

def classify_gesture(result) -> str:
    """
    Classify gesture from HandLandmarker results.

    Webcam is mirrored:
      MediaPipe "Right" → player's LEFT hand
      MediaPipe "Left"  → player's RIGHT hand
    """
    if not result.hand_landmarks or not result.handedness:
        return "idle"

    hand_data = {}
    for hand_landmarks, handedness in zip(
        result.hand_landmarks, result.handedness
    ):
        label = handedness[0].category_name
        hand_data[label] = hand_landmarks

    left_lm = hand_data.get("Left")    # player's RIGHT hand
    right_lm = hand_data.get("Right")  # player's LEFT hand

    # Rule 1: both hands open & raised → "reach"
    if left_lm and right_lm:
        if (is_hand_open(left_lm) and is_hand_raised(left_lm) and
                is_hand_open(right_lm) and is_hand_raised(right_lm)):
            return "reach"

    # Rule 2: MediaPipe "Right" (player's LEFT) index only → run LEFT
    if right_lm and is_index_only(right_lm):
        if not (left_lm and is_index_only(left_lm)):
            return "left"

    # Rule 3: MediaPipe "Left" (player's RIGHT) index only → run RIGHT
    if left_lm and is_index_only(left_lm):
        if not (right_lm and is_index_only(right_lm)):
            return "right"

    return "idle"


# ────────────────────────────────────────────────────────────────────
#  Drawing helpers
# ────────────────────────────────────────────────────────────────────

HAND_CONNECTIONS = [
    (0, 1), (1, 2), (2, 3), (3, 4),
    (0, 5), (5, 6), (6, 7), (7, 8),
    (0, 9), (9, 10), (10, 11), (11, 12),
    (0, 13), (13, 14), (14, 15), (15, 16),
    (0, 17), (17, 18), (18, 19), (19, 20),
    (5, 9), (9, 13), (13, 17),
]


def draw_landmarks_on_frame(frame, result):
    if not result.hand_landmarks:
        return
    h, w, _ = frame.shape
    for hand_landmarks in result.hand_landmarks:
        for s, e in HAND_CONNECTIONS:
            x1, y1 = int(hand_landmarks[s].x * w), int(hand_landmarks[s].y * h)
            x2, y2 = int(hand_landmarks[e].x * w), int(hand_landmarks[e].y * h)
            cv2.line(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
        for lm in hand_landmarks:
            cv2.circle(frame, (int(lm.x * w), int(lm.y * h)), 4, (0, 0, 255), -1)


# ────────────────────────────────────────────────────────────────────
#  Main loop
# ────────────────────────────────────────────────────────────────────

def main(port: int):
    # UDP socket for sending to Unity
    udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    dest = ("127.0.0.1", port)
    print(f"[PoseServer] Sending gesture data via UDP to localhost:{port}")

    # Webcam
    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        print("[PoseServer] ERROR: Cannot open webcam.")
        return

    print("[PoseServer] Webcam opened. Press 'q' in the preview window to quit.")

    # MediaPipe Hand Landmarker (Tasks API)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    model_path = os.path.join(script_dir, "hand_landmarker.task")

    if not os.path.exists(model_path):
        print(f"[PoseServer] ERROR: Model not found at {model_path}")
        print("[PoseServer] Download: https://storage.googleapis.com/mediapipe-models/"
              "hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task")
        cap.release()
        return

    base_options = mp_python.BaseOptions(model_asset_path=model_path)
    options = vision.HandLandmarkerOptions(
        base_options=base_options,
        running_mode=vision.RunningMode.VIDEO,
        num_hands=2,
        min_hand_detection_confidence=0.6,
        min_hand_presence_confidence=0.5,
        min_tracking_confidence=0.5,
    )
    landmarker = vision.HandLandmarker.create_from_options(options)
    frame_timestamp_ms = 0

    try:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

            frame_timestamp_ms += 33  # ~30 FPS
            result = landmarker.detect_for_video(mp_image, frame_timestamp_ms)

            gesture = classify_gesture(result)

            # Send to Unity via UDP
            message = json.dumps({"gesture": gesture}).encode("utf-8")
            udp_sock.sendto(message, dest)

            # Draw preview
            draw_landmarks_on_frame(frame, result)
            color = {
                "right": (255, 200, 0),
                "left": (0, 200, 255),
                "reach": (0, 255, 0),
                "idle": (128, 128, 128),
            }.get(gesture, (128, 128, 128))

            cv2.putText(frame, f"Gesture: {gesture.upper()}", (10, 40),
                        cv2.FONT_HERSHEY_SIMPLEX, 1.2, color, 3)
            cv2.putText(frame, f"Sending to UDP :{port}", (10, 80),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.7, (200, 200, 200), 2)
            cv2.imshow("Starlight Guardian - Pose Detection", frame)

            if cv2.waitKey(5) & 0xFF == ord("q"):
                break
    finally:
        landmarker.close()
        cap.release()
        cv2.destroyAllWindows()
        udp_sock.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Starlight Guardian Pose Server (UDP)")
    parser.add_argument("--port", type=int, default=8765, help="UDP port (default: 8765)")
    args = parser.parse_args()
    main(args.port)
