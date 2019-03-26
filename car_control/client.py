import socket
import threading
from pynput import keyboard

HOST = "192.168.0.196"
PORT = 12345

try:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    print("Socket successfully created")
except socket.error as err:
    print("socket creation failed with error {}".format(err))

s.connect((HOST, PORT))
print("Connected succesfully!")
print(s.recv(5000))

pressed = {"left": False, "right": False, "up": False, "down": False}


def set_interval(func, sec):
    def func_wrapper():
        set_interval(func, sec)
        func()

    t = threading.Timer(sec, func_wrapper)
    t.start()
    return t


def on_press(key):
    if not hasattr(key, "name"):
        return
    k = key.name

    if k == "esc":
        s.close()
        lis.stop()
    if k in pressed.keys():
        print("Key pressed:", k)
        pressed[k] = True


def on_release(key):
    if not hasattr(key, "name"):
        return
    k = key.name

    if k in pressed.keys():
        print("Key released:", k)
        pressed[k] = False


def send_pressed():
    for p in pressed:
        if pressed[p]:
            s.send(p.encode("utf-8"))


set_interval(send_pressed, 0.1)

lis = keyboard.Listener(on_press=on_press, on_release=on_release)
lis.start()
lis.join()
