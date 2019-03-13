#!/user/bin/env python
from queue import Queue
from serial import Serial
from time import sleep


port = "/dev/ttyACM0"


class ArduinoInterface:
    def __init__(self, queue: Queue):
        self.arduino = Serial(port, 9600)
        self.arduino.flushInput()
        self.queue = queue
        self.sent = 0
        self.received = 0
        print("Successfully initialized ArduinoInterface")

    def start(self):
        self.running = True
        print("Opening Connection to the Arduino")
        while self.running:
            if not self.queue.empty():
                message = self.queue.get()
                if message == b"up":
                    self.arduino.write(b"f")
                    self.sent += 1
                if message == b"right":
                    self.arduino.write(b"r")
                    self.sent += 1
                if message == b"down":
                    self.arduino.write(b"b")
                    self.sent += 1
                if message == b"left":
                    self.arduino.write(b"l")
                    self.sent += 1
                if message == b"low":
                    self.arduino.write(b"1")
                    self.sent += 1
                if message == b"medium":
                    self.arduino.write(b"2")
                    self.sent += 1
                if message == b"high":
                    self.arduino.write(b"3")
                    self.sent += 1
            if self.arduino.inWaiting() > 0:
                raw = self.arduino.read(self.arduino.inWaiting())
                try:
                    response = raw.decode()
                except UnicodeDecodeError:
                    response = raw
                print(f'Message from Arduino: "{response}"')
                self.received += 1
        print("Closing Connection to the Arduino")
