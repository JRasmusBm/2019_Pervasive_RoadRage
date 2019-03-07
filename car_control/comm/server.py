import socket

from queue import Queue

HOST = ""
PORT = 12345


class Server:
    def __init__(self, queue: Queue):
        self.queue = queue
        self.socket = socket.socket()
        print("Socket successfully created")
        self.socket.bind(("", PORT))
        print("Socket bound to {}".format(PORT))
        self.running = False

    def start(self):
        self.socket.listen(5)
        print("Socket is now listening")
        self.running = True
        c, addr = self.socket.accept()
        print("Got connection from", addr)
        c.send(b"Thank you for connecting")
        self.connected = True

        while self.running and self.connected:
            message = c.recv(5000)
            if message == b"esc":
                c.close()
                self.connected = False
            else:
                self.queue.put(message)
