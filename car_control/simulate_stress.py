import socket
from pynput import keyboard

HOST = "192.168.0.196"
PORT = 12346

try:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    print("Socket successfully created")
except socket.error as err:
    print("socket creation failed with error {}".format(err))

s.connect((HOST, PORT))
print("Connected succesfully!")
print(s.recv(5000))


def on_press(key):
    if hasattr(key, "char"):
        k = key.char
    elif hasattr(key, "name"):
        k = key.name
    else:
        return
    if k == "esc":
        s.close()
        lis.stop()
    if k in [1, "1", "11"]:
        print("low")
        s.send(b"low")
    if k in [2, "2", "22"]:
        print("medium")
        s.send(b"medium")
    if k in [3, "3", "33"]:
        print("high")
        s.send(b"high")


lis = keyboard.Listener(on_press=on_press)
lis.start()
lis.join()
