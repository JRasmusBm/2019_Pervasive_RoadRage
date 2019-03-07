import socket
from pynput import keyboard

HOST = "192.168.1.35"
PORT = 12345

try:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    print("Socket successfully created")
except socket.error as err:
    print("socket creation failed with error {}".format(err))

s.connect((HOST, PORT))
print("Connected succesfully!")
print(s.recv(5000))

allowed_keys = ("left", "right", "up", "down", "esc")


def on_press(key):
    if not hasattr(key, "name"):
        return
    k = key.name

    if k in allowed_keys:
        print("Key pressed:", k)
        s.send(k.encode("utf-8"))
        if k == "esc":
            s.close()
            lis.stop()


lis = keyboard.Listener(on_press=on_press)
lis.start()
lis.join()
