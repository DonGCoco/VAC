using UnityEngine;
using VACExperiment.Tetris;

namespace VACExperiment
{
    public class KeyboardDebugInput : MonoBehaviour
    {
        [SerializeField] private TetrisManager tetrisManager;
        [SerializeField] private DepthJudgmentManager depthJudgmentManager;

        private void Update()
        {
            if (depthJudgmentManager != null && depthJudgmentManager.IsRunning)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                    depthJudgmentManager.SubmitLeft();

                if (Input.GetKeyDown(KeyCode.RightArrow))
                    depthJudgmentManager.SubmitRight();

                return;
            }

            if (tetrisManager == null || !tetrisManager.IsRunning)
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                tetrisManager.MoveLeft();

            if (Input.GetKeyDown(KeyCode.RightArrow))
                tetrisManager.MoveRight();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                tetrisManager.RotateClockwise();

            if (Input.GetKeyDown(KeyCode.Space))
                tetrisManager.HardDrop();

            tetrisManager.SetSoftDrop(Input.GetKey(KeyCode.DownArrow));
        }
    }
}
