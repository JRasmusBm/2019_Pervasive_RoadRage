#include <Servo.h>

Servo steering;
Servo throttle;
char FORWARD = 'f';
char RIGHT = 'r';
char BACK = 'b';
char LEFT = 'l';
char LOW_STRESS = '1';
char MEDIUM_STRESS = '2';
char HIGH_STRESS = '3';
unsigned long lastSteeringInput = 0;
unsigned long lastThrottleInput = 0;
unsigned long lastOutput = 0;
char throttleState = 'S';
char steeringState = 'C';
char stressState = '2';

void updateThrottle() {
  Serial.print(throttleState);
  Serial.print(stressState);
  switch(throttleState) {
    case 'F':
      if (stressState == LOW_STRESS) {
        throttle.writeMicroseconds(1650);
      }
      if (stressState == MEDIUM_STRESS) {
        throttle.writeMicroseconds(1600);
      }
      if (stressState == HIGH_STRESS) {
        throttle.writeMicroseconds(1560);
      }
      break;
    case 'B':
      if (stressState == LOW_STRESS) {
        throttle.writeMicroseconds(1300);
      }
      if (stressState == MEDIUM_STRESS) {
        throttle.writeMicroseconds(1350);
      }
      if (stressState == HIGH_STRESS) {
        throttle.writeMicroseconds(1400);
      }
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
  if (input == LOW_STRESS) {
    stressState = LOW_STRESS;
  }
  if (input == MEDIUM_STRESS) {
    stressState = MEDIUM_STRESS;
  }
  if (input == HIGH_STRESS) {
    stressState = HIGH_STRESS;
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
  if (millis() - lastThrottleInput > 300) {
    resetThrottle();
  }
  if (millis() - lastSteeringInput > 500) {
    resetSteering();
  }
  if (millis() - lastOutput > 100) {
    lastOutput = millis();
    updateThrottle();
    updateSteering();
  }
}
