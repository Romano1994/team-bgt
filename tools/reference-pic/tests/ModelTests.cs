using System.Collections.Generic;
using System.Drawing;

namespace ReferencePic.Tests
{
    public static class ModelTests
    {
        public static void Run()
        {
            // Undo: 체크포인트 후 추가한 항목은 Undo로 사라짐
            Document doc = new Document();
            doc.Checkpoint();
            DrawObj pen = new DrawObj { Kind = Kind.Pen, Points = new List<PointF> { new PointF(1, 1) } };
            doc.Items.Add(pen);
            T.Check(doc.Undo(), "Undo 1차 true");
            T.Check(doc.Items.Count == 0, "Undo로 추가분 제거");
            T.Check(!doc.Undo(), "더 이상 되돌릴 것 없으면 false");

            // 깊은 복사: 점 1개 펜 추가 후 체크포인트, 점 추가, Undo하면 점 1개로 복원
            Document doc2 = new Document();
            DrawObj pen2 = new DrawObj { Kind = Kind.Pen, Points = new List<PointF> { new PointF(5, 5) } };
            doc2.Items.Add(pen2);
            doc2.Checkpoint();
            pen2.Points.Add(new PointF(9, 9));
            T.Check(doc2.Undo(), "Undo 2차 true");
            T.Check(doc2.Items.Count == 1 && doc2.Items[0].Points.Count == 1, "Undo는 체크포인트 시점 깊은 복사본으로 복원");

            // 이력 상한 100: 105번 체크포인트 -> Undo 100번 true, 101번째부터 false
            Document doc3 = new Document();
            for (int i = 0; i < 105; i++) doc3.Checkpoint();
            int okCount = 0;
            for (int i = 0; i < 100; i++) if (doc3.Undo()) okCount++;
            T.Check(okCount == 100, "체크포인트는 최대 100개까지만 쌓임(Undo 100번 true)");
            T.Check(!doc3.Undo(), "101번째 Undo는 false");

            // 다시 편집은 제자리에서 갱신, Undo는 원래(편집 전) 상태로
            DrawObj text = new DrawObj { Kind = Kind.Text, Lines = Lines("원래"), Hidden = true };
            DrawObj erase = new DrawObj { Kind = Kind.Erase };
            Document doc4 = new Document();
            doc4.Items.Add(text);
            doc4.Items.Add(erase);
            List<List<Run>> newLines = Lines("새글");
            doc4.CommitText(text, new PointF(0, 0), Color.Red, newLines, new SizeF(10, 10));
            T.Check(doc4.Items[0] == text, "CommitText 편집 중 항목은 같은 참조 유지");
            T.Check(doc4.Items[0].Lines == newLines, "CommitText로 Lines 교체");
            T.Check(!doc4.Items[0].Hidden, "CommitText 후 Hidden==false");
            T.Check(doc4.Items[1] == erase, "다른 항목은 그대로");

            T.Check(doc4.Undo(), "CommitText 후 Undo true");
            T.Check(doc4.Items[0].Lines[0][0].Text == "원래", "Undo로 원래 Lines 복원");
            T.Check(!doc4.Items[0].Hidden, "Undo 복원본도 Hidden==false(Clone 규칙)");

            // 빈 내용으로 확정 - 새 텍스트: 아무 것도 안 바뀌고 Checkpoint도 안 남음
            Document doc5 = new Document();
            doc5.CommitText(null, new PointF(0, 0), Color.Red, Blank(), new SizeF(0, 0));
            T.Check(doc5.Items.Count == 0, "새 텍스트가 빈 내용이면 추가 안 함");
            T.Check(!doc5.Undo(), "빈 새 텍스트 확정은 Checkpoint를 남기지 않음");

            // 빈 내용으로 확정 - 재편집: 그 항목 제거(Checkpoint는 남김)
            DrawObj editing2 = new DrawObj { Kind = Kind.Text, Lines = Lines("지울것") };
            Document doc6 = new Document();
            doc6.Items.Add(editing2);
            doc6.CommitText(editing2, new PointF(0, 0), Color.Red, Blank(), new SizeF(0, 0));
            T.Check(doc6.Items.Count == 0, "재편집 중 빈 내용 확정은 항목 제거");
            T.Check(doc6.Undo(), "제거도 Checkpoint를 남김");
            T.Check(doc6.Items.Count == 1, "Undo로 제거가 복원됨");
        }

        private static List<List<Run>> Lines(string s)
        {
            Run r = new Run();
            r.Text = s;
            r.Color = Color.Black;
            return new List<List<Run>> { new List<Run> { r } };
        }

        private static List<List<Run>> Blank()
        {
            return new List<List<Run>>();
        }
    }
}
