#include <Servo.h>

Servo steering;
Servo throttle;
char FORWARD = 'f';
char RIGHT = 'r';
char BACK = 'b';
char LEFT = 'l';
unsigned long lastSteeringInput = 0;
unsigned long lastThrottleInput = 0;
unsigned long lastOutput = 0;
char throttleState = 'S';
char steeringState = 'C';

void updateThrottle() {
  Serial.print(throttleState);
  switch(throttleState) {
    case 'F':
      throttle.writeMicroseconds(1625);
      break;
    case 'B':
      throttle.writeMicroseconds(1350);
      break;
    default:
      throttle.writeMicroseconds(1500);
  }
}

void updateSteering() {
  Serial.print(steeringState);
  switch(steeringState) {
    case 'L':
      steering.writeMicroseconds(1900);
      break;
    case 'R':
      steering.writeMicroseconds(1300);
      break;
    default:
      steering.writeMicroseconds(1600);
  }
}

void updateState(char input) {
  if (input == FORWARD) {
    lastThrottleInput = millis();
    throttleState = throttleState == 'B' ? 'S' : 'F';
  }
  if (input == BACK) {
    lastThrottleInput = millis();
    throttleState = throttleState == 'B' ? 'S' : 'B';
  }
  if (input == RIGHT) {
    lastSteeringInput = millis();
    steeringState = 'R';
  }
  if (input == LEFT) {
    lastSteeringInput = millis();
    steeringState = 'L';
  }
}
void resetThrottle() {
  throttleState = 'S';
}

void resetSteering() {
  steeringState = 'C';
}

void setup() {
  throttle.attach(2);
  steering.attach(3);
  Serial.begin(9600);
}

void loop() {
  if (Serial.available() > 0) {
    updateState(Serial.read());
  }
  if (millis() - lastThrottleInput > 1000) {
    resetThrottle();
  }
  if (millis() - lastSteeringInput > 1000) {
    resetSteering();
  }
  if (millis() - lastOutput > 100) {
    lastOutput = millis();
    updateThrottle();
    updateSteering();
  }
}
