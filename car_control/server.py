from threading import Thread
from queue import Queue
from comm.server import Server
from comm.stress_server import StressServer
from arduino.ArduinoInterface import ArduinoInterface


def main():
    print("Initializing Queue...")
    queue = Queue()

    s = Server(queue)
    ss = StressServer(queue)
    a = ArduinoInterface(queue)

    t1 = Thread(target=s.start)
    t2 = Thread(target=a.start)
    t3 = Thread(target=ss.start)

    t1.start()
    t2.start()
    t3.start()

    while True:
        pass


main()
