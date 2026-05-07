# -*- coding: utf-8 -*-

import json
import os
import socket
import time


GESTURE_MAP = {
    0: "rest",
    1: "fist",
    2: "pinch",
    3: "open",
}

KEY_TO_GESTURE = {
    "4": 0,
    "5": 1,
    "6": 2,
    "7": 3,
}

HOST = "127.0.0.1"
PORT = 8080


class GlobalKeyReader:
    def __init__(self):
        self._is_windows = os.name == "nt"
        self._previous_down = {key: False for key in KEY_TO_GESTURE}
        self._quit_was_down = False

        if self._is_windows:
            import ctypes

            self._get_async_key_state = ctypes.windll.user32.GetAsyncKeyState
        else:
            self._get_async_key_state = None

    def poll_gesture(self):
        if self._is_windows:
            return self._poll_windows_global_keys()

        key = input("Press 4-7 to send gesture, q to quit: ").strip().lower()
        if key == "q":
            raise KeyboardInterrupt
        return KEY_TO_GESTURE.get(key)

    def _poll_windows_global_keys(self):
        if self._is_key_down("q"):
            if not self._quit_was_down:
                raise KeyboardInterrupt
            self._quit_was_down = True
        else:
            self._quit_was_down = False

        for key, gesture_id in KEY_TO_GESTURE.items():
            is_down = self._is_key_down(key)
            was_down = self._previous_down[key]
            self._previous_down[key] = is_down

            if is_down and not was_down:
                return gesture_id

        return None

    def _is_key_down(self, key):
        virtual_key = ord(key.upper())
        return (self._get_async_key_state(virtual_key) & 0x8000) != 0


def make_fake_frame(frame_id, gesture_id):
    gesture_name = GESTURE_MAP.get(gesture_id, "unknown")

    if gesture_id == 0:
        confidence = 1.0
        rms = 2.0
        force = 0.0
    else:
        confidence = 0.95
        rms = 18.0 + gesture_id * 3.0
        force = min(max((rms - 5.0) / 25.0 * 100.0, 0.0), 100.0)

    return {
        "gesture": f"gesture_{gesture_id}",
        "gesture_id": gesture_id,
        "gesture_name": gesture_name,
        "gesture_confidence": round(confidence, 4),
        "current_rms": round(rms, 4),
        "current_rms_smooth": round(rms, 4),
        "current_force": round(force, 2),
        "current_force_smooth": round(force, 2),
    }


def main():
    key_reader = GlobalKeyReader()

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        s.bind((HOST, PORT))
        s.listen()
        print(f"Server started waiting for connection...: {HOST}:{PORT}")
        conn, addr = s.accept()
        with conn:
            print(f"Connected with Unity by {addr}")
            print("Press 4/5/6/7 to send gesture_0/1/2/3. Press q to quit.")

            frame_id = 0
            while True:
                gesture_id = key_reader.poll_gesture()
                if gesture_id is None:
                    time.sleep(0.02)
                    continue

                data = make_fake_frame(frame_id, gesture_id)
                msg = json.dumps(data, ensure_ascii=False) + "\n"
                conn.sendall(msg.encode("utf-8"))
                print(f"Sent: {msg.strip()}")
                frame_id += 1
                time.sleep(0.05)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nStopped.")
