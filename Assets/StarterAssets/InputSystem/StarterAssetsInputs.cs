using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool scroll;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		[Header("Input Block")]
		public bool isInputLocked = false;

		private void Update()
		{
			if (move != Vector2.zero || isInputLocked)
			{
				Debug.Log($"<color=cyan>[Input Update] Move: {move}, isInputLocked: {isInputLocked}</color>");
			}
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			Vector2 moveVal = value.Get<Vector2>();
			Debug.Log($"<color=orange>[Input] OnMove Recibido: {moveVal}. Bloqueado: {isInputLocked}</color>");
			
			if (isInputLocked)
			{
				move = Vector2.zero;
				return;
			}
			MoveInput(moveVal);
		}

		public void OnLook(InputValue value)
		{
			Vector2 lookVal = value.Get<Vector2>();
			// Log solo si hay movimiento para no saturar, o log de estado si está bloqueado
			if (lookVal.sqrMagnitude > 0.01f)
			{
				Debug.Log($"<color=yellow>[Input] OnLook Recibido: {lookVal}. Bloqueado: {isInputLocked}, CursorForLook: {cursorInputForLook}</color>");
			}

			if (isInputLocked)
			{
				look = Vector2.zero;
				return;
			}
			if(cursorInputForLook)
			{
				LookInput(lookVal);
			}
		}

		public void OnJump(InputValue value)
		{
			if (isInputLocked)
			{
				jump = false;
				return;
			}
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			if (isInputLocked)
			{
				sprint = false;
				return;
			}
			SprintInput(value.isPressed);
		}

		public void OnScroll(InputValue value)
		{
			if (isInputLocked) return;
			ScrollInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void ScrollInput(bool newScrollState)
		{
			scroll = newScrollState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}