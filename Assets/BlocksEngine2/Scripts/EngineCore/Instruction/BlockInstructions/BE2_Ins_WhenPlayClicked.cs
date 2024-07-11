using MG_BlocksEngine2.Block.Instruction;
using MG_BlocksEngine2.Core;
using UnityEngine;
using System.Collections;
public class BE2_Ins_WhenPlayClicked : BE2_InstructionBase, I_BE2_Instruction {
    protected override void OnButtonPlay()
    {
        Debug.Log("Play button clicked");
        BlocksStack.IsActive = true;
    }

    protected override void OnAwake()
    {
        BlocksStack.OnStackLastBlockExecuted.AddListener(EndExecution);
    }

    void EndExecution()
    {
        Debug.Log("EndExecution called");
        BlocksStack.IsActive = false;
    }

    public new void Function()
    {
        Debug.Log("Function called in BE2_Ins_WhenPlayClicked");
        StartCoroutine(ExecuteWithDelay());
    }

    private IEnumerator ExecuteWithDelay()
    {
        yield return new WaitForSeconds(1f); // Add a small delay to ensure execution
        Debug.Log("Executing section 0 after delay");
        ExecuteSection(0);
    }
}