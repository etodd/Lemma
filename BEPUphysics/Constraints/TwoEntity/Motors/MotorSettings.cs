using System;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints.TwoEntity.Motors
{
    /// <summary>
    /// Defines the behavior style of a motor.
    /// </summary>
    public enum MotorMode
    {
        /// <summary>
        /// Velocity motors only work to try to reach some relative velocity.
        /// They have no position goal.
        /// 
        /// When this type is selected, the motor settings' velocityMotor data will be used.
        /// </summary>
        VelocityMotor,
        /// <summary>
        /// Servomechanisms change their velocity in order to reach some position goal.
        /// 
        /// When this type is selected, the motor settings' servo data will be used.
        /// </summary>
        Servomechanism
    }

    /// <summary>
    /// Contains genereal settings for motors.
    /// </summary>
    public abstract class MotorSettings
    {
        internal EntitySolverUpdateable motor;

        internal float maximumForce = float.MaxValue;
        internal MotorMode mode = MotorMode.VelocityMotor;

        internal MotorSettings(EntitySolverUpdateable motor)
        {
            this.motor = motor;
        }

        /// <summary>
        /// Gets and sets the maximum impulse that the constraint will attempt to apply when satisfying its requirements.
        /// This field can be used to simulate friction in a constraint.
        /// </summary>
        public float MaximumForce
        {
            get
            {
                if (maximumForce > 0)
                {
                    return maximumForce;
                }
                return 0;
            }
            set
            {
                value = value >= 0 ? value : 0;
                if (value != maximumForce)
                {
                    maximumForce = value;
                    WakeUpEntities();
                }
            }
        }

        /// <summary>
        /// Gets or sets what kind of motor this is.
        /// 
        /// If velocityMotor is chosen, the motor will try to achieve some velocity using the VelocityMotorSettings.
        /// If servomechanism is chosen, the motor will try to reach some position using the ServoSettings.
        /// </summary>
        public MotorMode Mode
        {
            get { return mode; }
            set
            {
                if (mode != value)
                {
                    mode = value;
                    WakeUpEntities();
                }
            }
        }

        internal void WakeUpEntities()
        {
            for (int i = 0; i < motor.involvedEntities.count; i++)
            {
                if (motor.involvedEntities[i].isDynamic)
                {
                    //Only need to wake up one dynamic entity.  That will wake up the rest.
                    //Wouldn't want to pointlessly force-wake a kinematic object.
                    motor.involvedEntities[i].activityInformation.Activate();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Contains settings for motors which act on one degree of freedom.
    /// </summary>
    public class MotorSettings1D : MotorSettings
    {
        internal ServoSettings1D servo;
        internal VelocityMotorSettings1D velocityMotor;

        internal MotorSettings1D(Motor motor)
            : base(motor)
        {
            servo = new ServoSettings1D(this);
            velocityMotor = new VelocityMotorSettings1D(this);
        }

        /// <summary>
        /// Gets the settings that govern the behavior of this motor if it is a servomechanism.
        /// </summary>
        public ServoSettings1D Servo
        {
            get { return servo; }
        }

        /// <summary>
        /// Gets the settings that govern the behavior of this motor if it is a velocity motor.
        /// </summary>
        public VelocityMotorSettings1D VelocityMotor
        {
            get { return velocityMotor; }
        }
    }

    /// <summary>
    /// Contains settings for motors which act on three degrees of freedom.
    /// </summary>
    public class MotorSettings3D : MotorSettings
    {
        internal ServoSettings3D servo;
        internal VelocityMotorSettings3D velocityMotor;

        internal MotorSettings3D(EntitySolverUpdateable motor)
            : base(motor)
        {
            servo = new ServoSettings3D(this);
            velocityMotor = new VelocityMotorSettings3D(this);
        }

        /// <summary>
        /// Gets the settings that govern the behavior of this motor if it is a servomechanism.
        /// </summary>
        public ServoSettings3D Servo
        {
            get { return servo; }
        }

        /// <summary>
        /// Gets the settings that govern the behavior of this motor if it is a velocity motor.
        /// </summary>
        public VelocityMotorSettings3D VelocityMotor
        {
            get { return velocityMotor; }
        }
    }

    /// <summary>
    /// Contains settings for motors which act on two entities' relative orientation.
    /// </summary>
    public class MotorSettingsOrientation : MotorSettings
    {
        internal ServoSettingsOrientation servo;
        internal VelocityMotorSettings3D velocityMotor;

        internal MotorSettingsOrientation(EntitySolverUpdateable motor)
            : base(motor)
        {
            servo = new ServoSettingsOrientation(this);
            velocityMotor = new VelocityMotorSettings3D(this);
        }

        /// <summary>
        /// Gets the settings that govern the behavior of this motor if it is a servomechanism.
        /// </summary>
        public ServoSettingsOrientation Servo
        {
            get { return servo; }
        }

        /// <summary>
        /// Gets the settings that govern the behavior of this motor if it is a velocity motor.
        /// </summary>
        public VelocityMotorSettings3D VelocityMotor
        {
            get { return velocityMotor; }
        }
    }

    /// <summary>
    /// Defines the behavior of a servo.
    /// Used when the MotorSettings' motorType is set to servomechanism.
    /// </summary>
    public class ServoSettings : ISpringSettings
    {
        internal MotorSettings motorSettings;

        /// <summary>
        /// Speed at which the servo will try to achieve its goal.
        /// </summary>
        internal float baseCorrectiveSpeed;

        /// <summary>
        /// Maximum extra velocity that the constraint will apply in an effort to correct constraint error.
        /// </summary>
        internal float maxCorrectiveVelocity = float.MaxValue;

        /// <summary>
        /// Squared maximum extra velocity that the constraint will apply in an effort to correct constraint error.
        /// </summary>
        internal float maxCorrectiveVelocitySquared = float.MaxValue;

        /// <summary>
        /// Spring settings define how a constraint responds to velocity and position error.
        /// </summary>
        internal SpringSettings springSettings = new SpringSettings();

        internal ServoSettings(MotorSettings motorSettings)
        {
            this.motorSettings = motorSettings;
        }

        /// <summary>
        /// Gets and sets the speed at which the servo will try to achieve its goal.
        /// This is inactive if the constraint is not in servo mode.
        /// </summary>
        public float BaseCorrectiveSpeed
        {
            get { return baseCorrectiveSpeed; }
            set
            {
                value = value < 0 ? 0 : value;
                if (value != baseCorrectiveSpeed)
                {
                    baseCorrectiveSpeed = value;
                    motorSettings.WakeUpEntities();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum extra velocity that the constraint will apply in an effort to correct any constraint error.
        /// </summary>
        public float MaxCorrectiveVelocity
        {
            get { return maxCorrectiveVelocity; }
            set
            {
                value = Math.Max(0, value);
                if (maxCorrectiveVelocity != value)
                {
                    maxCorrectiveVelocity = value;
                    if (maxCorrectiveVelocity >= float.MaxValue)
                    {
                        maxCorrectiveVelocitySquared = float.MaxValue;
                    }
                    else
                    {
                        maxCorrectiveVelocitySquared = maxCorrectiveVelocity * maxCorrectiveVelocity;
                    }
                    motorSettings.WakeUpEntities();
                }


            }
        }

        #region ISpringSettings Members

        /// <summary>
        /// Gets the spring settings used by the constraint.
        /// Spring settings define how a constraint responds to velocity and position error.
        /// </summary>
        public SpringSettings SpringSettings
        {
            get { return springSettings; }
        }

        #endregion
    }

    /// <summary>
    /// Defines the behavior of a servo that works on one degree of freedom.
    /// Used when the MotorSettings' motorType is set to servomechanism.
    /// </summary>
    public class ServoSettings1D : ServoSettings
    {
        internal float goal;

        internal ServoSettings1D(MotorSettings motorSettings)
            : base(motorSettings)
        {
        }

        /// <summary>
        /// Gets or sets the goal position of the servo.
        /// </summary>
        public float Goal
        {
            get { return goal; }
            set
            {
                if (goal != value)
                {
                    goal = value;
                    motorSettings.WakeUpEntities();
                }
            }
        }


    }

    /// <summary>
    /// Defines the behavior of a servo that works on three degrees of freedom.
    /// Used when the MotorSettings' motorType is set to servomechanism.
    /// </summary>
    public class ServoSettings3D : ServoSettings
    {
        internal Vector3 goal;

        internal ServoSettings3D(MotorSettings motorSettings)
            : base(motorSettings)
        {
        }

        /// <summary>
        /// Gets or sets the goal position of the servo.
        /// </summary>
        public Vector3 Goal
        {
            get { return goal; }
            set
            {
                if (goal != value)
                {
                    goal = value;
                    motorSettings.WakeUpEntities();
                }
            }
        }
    }

    /// <summary>
    /// Defines the behavior of a servo that works on the relative orientation of two entities.
    /// Used when the MotorSettings' motorType is set to servomechanism.
    /// </summary>
    public class ServoSettingsOrientation : ServoSettings
    {
        internal Quaternion goal;

        internal ServoSettingsOrientation(MotorSettings motorSettings)
            : base(motorSettings)
        {
        }

        /// <summary>
        /// Gets or sets the goal orientation of the servo.
        /// </summary>
        public Quaternion Goal
        {
            get { return goal; }
            set
            {
                if (goal != value)
                {
                    goal = value;
                    motorSettings.WakeUpEntities();
                }
            }
        }
    }

    /// <summary>
    /// Defines the behavior of a velocity motor.
    /// Used when the MotorSettings' motorType is set to velocityMotor.
    /// </summary>
    public class VelocityMotorSettings
    {
        internal MotorSettings motorSettings;

        /// <summary>
        /// Softness of this constraint.
        /// Higher values of softness allow the constraint to be violated more.
        /// Must be greater than zero.
        /// Sometimes, if a joint system is unstable, increasing the softness of the involved constraints will make it settle down.
        /// </summary>
        internal float softness = .0001f;

        internal VelocityMotorSettings(MotorSettings motorSettings)
        {
            this.motorSettings = motorSettings;
        }

        /// <summary>
        /// Gets and sets the softness of this constraint.
        /// Higher values of softness allow the constraint to be violated more.
        /// Must be greater than zero.
        /// Sometimes, if a joint system is unstable, increasing the softness of the involved constraints will make it settle down.
        /// For motors, softness can be used to implement damping.  For a damping constant k, the appropriate softness is 1/k.
        /// </summary>
        public float Softness
        {
            get { return softness; }
            set
            {
                value = value < 0 ? 0 : value;
                if (softness != value)
                {
                    softness = value;
                    motorSettings.WakeUpEntities();
                }
            }
        }
    }

    /// <summary>
    /// Defines the behavior of a velocity motor that works on one degree of freedom.
    /// Used when the MotorSettings' motorType is set to velocityMotor.
    /// </summary>
    public class VelocityMotorSettings1D : VelocityMotorSettings
    {
        internal float goalVelocity;

        internal VelocityMotorSettings1D(MotorSettings motorSettings)
            : base(motorSettings)
        {
        }

        /// <summary>
        /// Gets or sets the goal velocity of the motor.
        /// </summary>
        public float GoalVelocity
        {
            get { return goalVelocity; }
            set
            {
                goalVelocity = value;
                motorSettings.WakeUpEntities();
            }
        }
    }

    /// <summary>
    /// Defines the behavior of a velocity motor that works on three degrees of freedom.
    /// Used when the MotorSettings' motorType is set to velocityMotor.
    /// </summary>
    public class VelocityMotorSettings3D : VelocityMotorSettings
    {
        internal Vector3 goalVelocity;

        internal VelocityMotorSettings3D(MotorSettings motorSettings)
            : base(motorSettings)
        {
        }

        /// <summary>
        /// Gets or sets the goal position of the servo.
        /// </summary>
        public Vector3 GoalVelocity
        {
            get { return goalVelocity; }
            set
            {
                goalVelocity = value;
                motorSettings.WakeUpEntities();
            }
        }
    }
}