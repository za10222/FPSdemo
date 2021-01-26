using Unity.Entities;
using UnityEngine;
// in order to circumvent API breakages that do not affect physics, some packages are removed from the project on CI
// any code referencing APIs in com.unity.inputsystem must be guarded behind UNITY_INPUT_SYSTEM_EXISTS
using UnityEngine.InputSystem;

namespace FPSdemo
{
    public class UserInput {
        public struct UserInputdate : IComponentData
        {
            public UserCommand userinput;
        }

        [AlwaysUpdateSystem]
        [UpdateInGroup(typeof(InitializationSystemGroup))]
        public class  UserInputUpdateSystem : SystemBase, InputActions.IPlayerActions
        {
            InputActions m_InputActions;
            EntityQuery m_UserInputdateQuery;

            public void OnFire(InputAction.CallbackContext context)
            {
                m_CharacterFiring = context.ReadValue<float>();
            }

            public void OnLook(InputAction.CallbackContext context)
            {
                m_CharacterLooking = context.ReadValue<Vector2>();
            }

            public void OnMove(InputAction.CallbackContext context)
            {
                m_CharacterMovement = context.ReadValue<Vector2>();
            }
            public void OnJump(InputAction.CallbackContext context)
            {
                m_CharacterJump = context.ReadValueAsButton();
            }


            Vector2 m_CharacterMovement;
            Vector2 m_CharacterLooking;

            float m_CharacterFiring;

            bool m_CharacterJump;
            //    bool m_CharacterJumped;


            protected override void OnCreate()
            {

                m_InputActions = new InputActions();
                m_InputActions.Player.SetCallbacks(this);
                m_CharacterMovement = default;
                m_CharacterLooking = default;
                m_CharacterFiring = default;
                m_CharacterJump = default;
                m_UserInputdateQuery = GetEntityQuery(typeof(UserInputdate));
            }

            protected override void OnStartRunning() => m_InputActions.Enable();

            protected override void OnStopRunning() => m_InputActions.Disable();

            public void AccumulateInput(ref UserCommand command)
            {
                command.Looking = m_CharacterLooking;
                command.Movement = m_CharacterMovement;
                command.buttons.Or(UserCommand.Button.Jump, m_CharacterJump);
            }
            protected override void OnUpdate()
            {
                // character controller
                if (m_UserInputdateQuery.CalculateEntityCount() == 0)
                    EntityManager.CreateEntity(typeof(UserInputdate));

                UserCommand userinput = new UserCommand();
                AccumulateInput(ref userinput);
                m_UserInputdateQuery.SetSingleton(new UserInputdate
                {
                    userinput = userinput
                });

                //if (m_CharacterGunInputQuery.CalculateEntityCount() == 0)
                //    EntityManager.CreateEntity(typeof(CharacterGunInput));

                //m_CharacterGunInputQuery.SetSingleton(new CharacterGunInput
                //{
                //    Looking = m_CharacterLooking,
                //    Firing = m_CharacterFiring,
                //});

                //m_CharacterJumped = false;

                // vehicle
                //if (m_VehicleInputQuery.CalculateEntityCount() == 0)
                //    EntityManager.CreateEntity(typeof(VehicleInput));

                //m_VehicleInputQuery.SetSingleton(new VehicleInput
                //{
                //    Looking = m_VehicleLooking,
                //    Steering = m_VehicleSteering,
                //    Throttle = m_VehicleThrottle,
                //    Change = m_VehicleChanged
                //});

                //m_VehicleChanged = 0;
            }
        }

    }

}
