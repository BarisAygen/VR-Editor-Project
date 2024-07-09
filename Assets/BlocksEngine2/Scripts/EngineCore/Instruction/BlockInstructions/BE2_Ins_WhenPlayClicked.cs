using MG_BlocksEngine2.Block.Instruction;
using MG_BlocksEngine2.Core;
using UnityEngine;
using System.Collections;
public class BE2_Ins_WhenPlayClicked : BE2_InstructionBase, I_BE2_Instruction {
    protected override void OnButtonPlay()
    {
        BlocksStack.IsActive = true;
    }

    protected override void OnAwake()
    {
        BlocksStack.OnStackLastBlockExecuted.AddListener(EndExecution);
    }

    void EndExecution()
    {
        BlocksStack.IsActive = false;
    }

    public new void Function()
    {
        StartCoroutine(ExecuteWithDelay());
    }
    
    private IEnumerator ExecuteWithDelay()
    {
        yield return new WaitForSeconds(1f); // Add a small delay to ensure execution
        ExecuteSection(0);
    }
}