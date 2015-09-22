using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
#if NETDUINO_1
using SecretLabs.NETMF.Hardware.Netduino;
#elif NETDUINO_MINI
using SecretLabs.NETMF.Hardware.NetduinoMini;
#endif

// See:
// http://itp.nyu.edu/physcomp/Labs/DCMotorControl
// http://10rem.net/blog/2010/09/27/netduino-basics-using-pulse-width-modulation-pwm
// http://forums.netduino.com/index.php?/topic/8513-how-to-generate-a-pwm-signal-with-c/
// http://www.pretzellogix.com/2011/03/10/exploring-the-netduino-4-make-leds-really-glow-or-fun-with-pwm/


namespace uk.andyjohnson0.Netduino.Drivers.Motor
{
    /// <summary>
    /// Netduino driver for am SN754410 H-bridge. This device is used for controlling motors.
    /// Motor speed is set using PWM on the H-bridge enable pin, not the forward/reverse pins. 
    /// </summary>
    public class HBridgeSN754410
    {
        private static Cpu.Pin[] PwmPins =
#if NETDUINO_1
        { Pins.GPIO_PIN_D5, Pins.GPIO_PIN_D6, Pins.GPIO_PIN_D9, Pins.GPIO_PIN_D10};
#elif NETDUINO_MINI
        { Pins.GPIO_PIN_17, Pins.GPIO_PIN_18, Pins.GPIO_PIN_19, Pins.GPIO_PIN_20 };
#endif

        /// <summary>
        /// Constructor. Initialise an HBridgeSN754410 object to drive a single motor.
        /// </summary>
        /// <param name="motor1En">GPIO pin connected to the enable pin for motor 1.</param>
        /// <param name="motor1Fwd">GPIO pin connected to the foreward pin for motor 1.</param>
        /// <param name="motor1Rev">GPIO pin connected to the reverse pin for motor 1.</param>
        public HBridgeSN754410(
            Cpu.Pin motor1En,
            Cpu.Pin motor1Fwd,
            Cpu.Pin motor1Rev)
        : this(motor1En, motor1Fwd, motor1Rev, Cpu.Pin.GPIO_NONE, Cpu.Pin.GPIO_NONE, Cpu.Pin.GPIO_NONE)
        {
        }


        /// <summary>
        /// Constructor. Initialise an HBridgeSN754410 object to drive two motors.
        /// </summary>
        /// <param name="motor1En">GPIO pin connected to the enable pin for motor 1.</param>
        /// <param name="motor1Fwd">GPIO pin connected to the foreward pin for motor 1.</param>
        /// <param name="motor1Rev">GPIO pin connected to the reverse pin for motor 1.</param>
        /// <param name="motor1En">GPIO pin connected to the enable pin for motor 2.</param>
        /// <param name="motor1Fwd">GPIO pin connected to the foreward pin for motor 2.</param>
        /// <param name="motor1Rev">GPIO pin connected to the reverse pin for motor 2.</param>
        public HBridgeSN754410(
            Cpu.Pin motor1En,
            Cpu.Pin motor1Fwd,
            Cpu.Pin motor1Rev,
            Cpu.Pin motor2En,
            Cpu.Pin motor2Fwd,
            Cpu.Pin motor2Rev)
        {
            if (motor1En != Cpu.Pin.GPIO_NONE)
            {
                if (Array.IndexOf(PwmPins, motor1En) == -1)
                {
                    throw new ArgumentException("Enable pin must be pwm-compatible", "motor1en");
                }
                this.motor1En = new PWM(motor1En);
                this.motor1En.SetPulse(pwmPeriod, 0);
            }
            if ((motor1Fwd != Cpu.Pin.GPIO_NONE) && (motor1Rev != Cpu.Pin.GPIO_NONE))
            {
                this.motor1Fwd = new OutputPort(motor1Fwd, false);
                this.motor1Rev = new OutputPort(motor1Rev, false);
            }

            if ( (motor2En != Cpu.Pin.GPIO_NONE) && (motor2En != motor1En) )
            {
                if (Array.IndexOf(PwmPins, motor1En) == -1)
                {
                    throw new ArgumentException("Enable pin must be pwm-compatible", "motor1en");
                }
                this.motor2En = new PWM(motor2En);
                this.motor2En.SetPulse(pwmPeriod, 0);
            }
            if ((motor2Fwd != Cpu.Pin.GPIO_NONE) && (motor2Rev != Cpu.Pin.GPIO_NONE))
            {
                this.motor2Fwd = new OutputPort(motor2Fwd, false);
                this.motor2Rev = new OutputPort(motor2Rev, false);
            }
        }


        private const uint pwmPeriod = 50000;  // PWM period in microseconds (1/1000000 sec)

        private PWM motor1En;
        private OutputPort motor1Fwd;
        private OutputPort motor1Rev;
        private uint motor1Speed = 0;

        private PWM motor2En;
        private OutputPort motor2Fwd;
        private OutputPort motor2Rev;
        private uint motor2Speed = 0;


        /// <summary>
        /// Defines the motor.
        /// </summary>
        [Flags]
        public enum Motor
        {
            /// <summary>No motor</summary>
            None = 0,
            /// <summary>First motor</summary>
            Motor1 = 1,
            /// <summary>Second motor</summary>
            Motor2 = 2
        }

        /// <summary>
        /// Defines motor direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>Stop motor rotation.</summary>
            Stop,
            /// <summary>Drive the motor foreward. The actual direction will depend on the physical motor connections.</summary>
            Foreward,
            /// <summary>Drive the motor backward. The actual direction will depend on the physical motor connections.</summary>
            Reverse
        }


        /// <summary>
        /// Set motor speed.
        /// </summary>
        /// <param name="motors">The motor.</param>
        /// <param name="speed">
        /// Speed as a percentage of maximum.
        /// Must be between 0 and 100 inclusive.
        /// </param>
        public void SetSpeed(
            Motor motors,
            int speed)
        {
            if ((speed < 0) || (speed > 100))
                throw new ArgumentOutOfRangeException("speed", "Value must be between 0 and 100");

            uint sf = pwmPeriod / 100;

            if (((motors & Motor.Motor1) == Motor.Motor1) && (motor1En != null))
            {
                motor1Speed = (uint)(speed * sf);
                motor1En.SetPulse(pwmPeriod, motor1Speed);
            }
            if (((motors & Motor.Motor2) == Motor.Motor2) && (motor2En != null))
            {
                motor2Speed = (uint)(speed * sf);
                motor2En.SetPulse(pwmPeriod, motor2Speed);
            }
        }


        /// <summary>
        /// Set motor direction.
        /// </summary>
        /// <param name="motors">The motor.</param>
        /// <param name="direction">The direction.</param>
        public void SetDirection(
            Motor motors,
            Direction direction)
        {
            switch(direction)
            {
                case Direction.Foreward:
                    if ((motors & Motor.Motor1) == Motor.Motor1)
                        SetMotor1(false, true);
                    if ((motors & Motor.Motor2) == Motor.Motor2)
                        SetMotor2(false, true);
                    break;
                case Direction.Reverse:
                    if ((motors & Motor.Motor1) == Motor.Motor1)
                        SetMotor1(true, false);
                    if ((motors & Motor.Motor2) == Motor.Motor2)
                        SetMotor2(true, false);
                    break;
                case Direction.Stop:
                    if ((motors & Motor.Motor1) == Motor.Motor1)
                        SetMotor1(false, false);
                    if ((motors & Motor.Motor2) == Motor.Motor2)
                        SetMotor2(false, false);
                    break;
            }
        }


        private void SetMotor1(bool a, bool b)
        {
            if ((motor1Fwd != null) && (motor1Rev != null))
            {
                motor1Fwd.Write(a);
                motor1Rev.Write(b);
            }
        }


        private void SetMotor2(bool a, bool b)
        {
            if ((motor2Fwd != null) && (motor2Rev != null))
            {
                motor2Fwd.Write(a);
                motor2Rev.Write(b);
            }
        }
    }
}
