using Unity.Barracuda;
using UnityEngine;
using System;
using System.Linq;



namespace Player.FightSystem.Magic {
public class SymbolRecognizer{
    private Model runtimeModel;
    private IWorker worker;

    public SymbolRecognizer(NNModel modelAsset)
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
    }
 
    ~SymbolRecognizer()
    {
        worker?.Dispose();
    }

    public float[] RecognizeSymbol(Texture2D inputImage)
    {
        Tensor inputTensor = new Tensor(inputImage, 3);
        worker.Execute(inputTensor);
        Tensor outputTensor = worker.PeekOutput();

        float[] outputArray = outputTensor.ToReadOnlyArray();
        inputTensor.Dispose();
        outputTensor.Dispose();
        return outputArray;
    }

    public (int symbol, float prediction) GetSymbol(Texture2D input)
    {
        float[] predictions = RecognizeSymbol(input);
        int predictedClass = Array.IndexOf(predictions, predictions.Max());

        return (predictedClass, predictions[predictedClass]);
    }
}
}