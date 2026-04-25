import socket
import time

HOST = '127.0.0.1'
PORT = 8080

gesture_map = ["gesture_1", "gesture_2"]

def main():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind((HOST, PORT))
        s.listen()
        print(f"Server started waiting for connection...: {HOST}:{PORT}")
        conn, addr = s.accept()
        with conn:
            print(f"Connected with Unity by {addr}")
            for gesture in gesture_map * 3:
                conn.sendall(gesture.encode())
                print(f"Sent {gesture} to Unity")
                time.sleep(1)
            print("Sent all gestures to Unity")


if __name__ == "__main__":
    main()