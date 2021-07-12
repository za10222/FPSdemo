using Unity.Entities;
using Unity.NetCode;
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

        public static Vector2 configMouseSensitivity= new Vector2(0.2f,0.2f);

        [DisableAutoCreation]
        [AlwaysUpdateSystem]
        [UpdateInGroup(typeof(GhostInputSystemGroup))]
        [UpdateBefore (typeof(PlayerInputCommandSystem))]

        public class  UserInputUpdateSystem : SystemBase, InputActions.IPlayerActions
        {
            InputActions m_InputActions;
            EntityQuery m_UserInputdateQuery; 

            public void OnFire(InputAction.CallbackContext context)
            {
                m_CharacterFiring = context.ReadValueAsButton();
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

            public void OnExit(InputAction.CallbackContext context)
            { 
                CursorTest.ToShowCursor();
            }
            public void OnChangeGun(InputAction.CallbackContext context)
            {
                switch (context.phase)
                {
                    case InputActionPhase.Performed:
                    m_ChangeGun = true;
                    break;
                }
            }

            Vector2 m_CharacterMovement;
            Vector2 m_CharacterLooking;


            bool m_CharacterFiring;
            bool m_CharacterJump;
            bool m_ChangeGun;

            //    bool m_CharacterJumped;


            protected override void OnCreate()
            {

                m_InputActions = new InputActions();
                m_InputActions.Player.SetCallbacks(this);
                m_CharacterMovement = default;
                m_CharacterLooking = default;
                m_CharacterFiring = default;
                m_CharacterJump = default;
                m_ChangeGun = default;
                m_UserInputdateQuery = GetEntityQuery(typeof(UserInputdate));
            }

            protected override void OnStartRunning() => m_InputActions.Enable();

            protected override void OnStopRunning() => m_InputActions.Disable();
            
            public void AccumulateInput(ref UserCommand command, float deltaTime)
            {
                Vector2 deltaMousePos = new Vector2(0, 0);
                if (deltaTime > 0.0f)
                    deltaMousePos += m_CharacterLooking;
                command.Looking.x += deltaMousePos.x * configMouseSensitivity.x;
                command.Looking.x = command.Looking.x % 360;
                while (command.Looking.x < 0.0f) command.Looking.x += 360.0f;

                command.Looking.y += deltaMousePos.y * configMouseSensitivity.y;
                command.Looking.y = Mathf.Clamp(command.Looking.y, 0, 180);

                command.Movement = m_CharacterMovement;
                command.buttons.Set(UserCommand.Button.Jump, m_CharacterJump);
                command.buttons.Set(UserCommand.Button.ChangeGun, m_ChangeGun);
                command.buttons.Set(UserCommand.Button.Shooting, m_CharacterFiring);

            }
            protected override void OnUpdate()
            {
                float dt = Time.DeltaTime;
                // character controller
                if (m_UserInputdateQuery.CalculateEntityCount() == 0)
                    EntityManager.CreateEntity(typeof(UserInputdate));
                var userinput = m_UserInputdateQuery.GetSingleton<UserInputdate>().userinput;
                AccumulateInput(ref userinput, dt);
                m_ChangeGun = false;

                m_UserInputdateQuery.SetSingleton(new UserInputdate
                {
                    userinput = userinput
                });

                //m_CharacterMovement = default;
                //m_CharacterLooking = default;
                //m_CharacterFiring = default;
                //m_CharacterJump = default;
                //m_ChangeGun = default;

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
