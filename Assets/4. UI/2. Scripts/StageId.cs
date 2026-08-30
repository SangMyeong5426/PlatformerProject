// 스테이지 식별자. 보스 클리어 플래그, 스테이지 포탈, 보석 UI 가 공유한다.
// 값을 명시해 둔 이유는 BoolManager 가 배열 인덱스로 쓰기 때문이다.
public enum StageId
{
    First = 0,  // 대지
    Second = 1, // 얼음
    Third = 2,  // 불
    Fourth = 3, // 바람
}
