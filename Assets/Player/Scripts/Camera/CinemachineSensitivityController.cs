using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Player.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineInputAxisController))]
    public class CinemachineSensitivityController : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 1f)]
        private float reducedMultiplier = 0.25f;

        private CinemachineInputAxisController axisController;
        private readonly List<ControllerSnapshot> trackedControllers = new();
        private bool isReduced;

        private void Awake()
        {
            axisController = GetComponent<CinemachineInputAxisController>();
            CacheControllers();
        }

        private void OnEnable()
        {
            CacheControllers();
            ApplyCurrentState();
        }

        private void OnDisable()
        {
            RestoreDefault();
            trackedControllers.Clear();
        }

        public void EnableReducedSensitivity()
        {
            if (isReduced)
                return;

            isReduced = true;
            ApplyCurrentState();
        }

        public void DisableReducedSensitivity()
        {
            if (!isReduced)
                return;

            isReduced = false;
            ApplyCurrentState();
        }

        private void CacheControllers()
        {
            trackedControllers.Clear();

            if (axisController == null)
                return;

            axisController.SynchronizeControllers();
            var controllers = axisController.Controllers;
            if (controllers == null)
                return;

            foreach (var controller in controllers)
            {
                if (controller?.Input == null)
                    continue;

                trackedControllers.Add(new ControllerSnapshot
                {
                    Controller = controller,
                    BaseGain = controller.Input.Gain
                });
            }
        }

        private void ApplyCurrentState()
        {
            if (trackedControllers.Count == 0)
                CacheControllers();

            foreach (var snapshot in trackedControllers)
            {
                if (snapshot.Controller?.Input == null)
                    continue;

                snapshot.Controller.Input.Gain = snapshot.BaseGain *
                    (isReduced ? reducedMultiplier : 1f);
            }
        }

        private void RestoreDefault()
        {
            foreach (var snapshot in trackedControllers)
            {
                if (snapshot.Controller?.Input == null)
                    continue;

                snapshot.Controller.Input.Gain = snapshot.BaseGain;
            }
        }

        private class ControllerSnapshot
        {
            public CinemachineInputAxisController.Controller Controller;
            public float BaseGain;
        }
    }
}
