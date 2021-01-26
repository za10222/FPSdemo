
using System;
using System.Text;
using Unity.Mathematics;

namespace FPSdemo
{
    public struct UserCommand
    {
        public enum Button : uint
        {
            None = 0,
            Jump = 1 << 0,
            //Boost = 1 << 1,
            //PrimaryFire = 1 << 2,
            //SecondaryFire = 1 << 3,
            //Reload = 1 << 4,
            //Melee = 1 << 5,
            //Use = 1 << 6,
            //Ability1 = 1 << 7,
            //Ability2 = 1 << 8,
            //Ability3 = 1 << 9,
            //Crouch = 1 << 10,

            //CameraSideSwitch = 1 << 15,

            //Item1 = 1 << 27,
            //Item2 = 1 << 28,
            //Item3 = 1 << 29,
            //Item4 = 1 << 30,
        }

        public struct ButtonBitField
        {
            public uint flags;

            public bool IsSet(Button button)
            {
                return (flags & (uint)button) > 0;
            }

            public void Or(Button button, bool val)
            {
                if (val)
                    flags = flags | (uint)button;
            }

            public void Set(Button button, bool val)
            {
                if (val)
                    flags = flags | (uint)button;
                else
                {
                    flags = flags & ~(uint)button;
                }
            }

            public override string ToString()
            {
                var stringBuilder = new StringBuilder();
                var names = Enum.GetNames(typeof(Button));
                var values = Enum.GetValues(typeof(Button));
                stringBuilder.Append("<");
                for (int i = 0; i < names.Length; i++)
                {
                    var value = (uint)values.GetValue(i);
                    if ((flags & value) == 0)
                        continue;

                    stringBuilder.Append("," + names[i]);
                }
                stringBuilder.Append(">");
                return stringBuilder.ToString();
            }
        }

        //public uint tick;
        //public int checkTick;        // For debug purposes
        //public int renderTick;
        public float2 Movement;
        public float2 Looking;
        public ButtonBitField buttons;

        public static readonly UserCommand defaultCommand = new UserCommand(0);

        private UserCommand(int i)
        {
            //tick = 0;
            //checkTick = 0;
            //renderTick = 0;
            Movement = default;
            Looking = default;
            buttons.flags = 0;
        }

        public void ClearCommand()
        {
            buttons.flags = 0;
            Movement = default;
            Looking = default;
        }
        public override string ToString()
        {
            System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
            strBuilder.AppendLine("moveYaw:" + Movement.x);
            strBuilder.AppendLine("moveMagnitude:" + Movement.y);
            strBuilder.AppendLine("lookYaw:" + Looking.x);
            strBuilder.AppendLine("lookPitch:" + Looking.y);
            strBuilder.AppendLine("buttons:" + buttons);
            return strBuilder.ToString();
        }
    }
}