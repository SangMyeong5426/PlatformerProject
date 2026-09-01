using System.IO;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// 에디터 Test Runner 로 돌린 결과를 파일로 남긴다.
//
// 왜 필요한가
//   배치모드(scripts/playmode-test)가 라이선스 문제로 막히면 에디터의 Test Runner 창에서
//   돌리게 된다. 그런데 **에디터 실행은 결과를 Editor.log 에 남기지 않는다.** 창에만
//   떠 있어서 사람이 눈으로 읽고 옮겨 적어야 하고, 옮겨 적는 순간 기록이 아니라 전언이 된다.
//
//   이 스크립트가 있으면 어느 쪽으로 돌리든 같은 형식(NUnit3 XML)의 결과 파일이 남는다.
//   scripts/playmode-test --last 로 읽는다.
//
// Logs/ 는 .gitignore 대상이다. 결과 파일은 커밋되지 않는다.
[InitializeOnLoad]
public static class TestResultWriter
{
    public const string OutputPath = "Logs/playmode-results.xml";

    static TestResultWriter()
    {
        // TestRunnerApi 는 ScriptableObject 다. 콜백은 도메인 리로드마다 다시 등록한다.
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Writer());
    }

    class Writer : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            string dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            TNode xml = result.ToXml();
            File.WriteAllText(OutputPath, xml.OuterXml);

            Debug.Log("[TestResultWriter] 결과를 " + OutputPath + " 에 썼다 - "
                      + "성공 " + result.PassCount + " 실패 " + result.FailCount
                      + " 건너뜀 " + result.SkipCount);
        }
    }
}
