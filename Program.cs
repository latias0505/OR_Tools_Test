using Google.OrTools.Sat;
using System;
using System.Collections.Generic;

namespace OR_Tools_Test
{
    internal class Program
    {
        // 상품 데이터 클래스
        class SKU
        {
            public string Name;   // 상품 명
            public int Weight;    // 상품 무게
            public int Frequency; // 30일 기준 누적 출고 수량
        }

        // 로케이션 데이터 클래스
        class Location
        {
            public int Id;              // 로케이션 ID
            public int DistanceToExit;  // 출구로부터 로케이션까지의 거리 정의
            public int MaxWeight = 100; // 로케이션 별 무게 제한 (테스트단계에서는 100kg으로 고정)
        }

        static void Main(string[] args)
        {
            // 샘플 SKU 데이터 정의
            var skus = new List<SKU>
        {
            new SKU { Name = "SKU1", Weight = 20, Frequency = 80 },
            new SKU { Name = "SKU2", Weight = 50, Frequency = 30 },
            new SKU { Name = "SKU3", Weight = 70, Frequency = 100 },
            new SKU { Name = "SKU4", Weight = 10, Frequency = 10 },
            new SKU { Name = "SKU5", Weight = 90, Frequency = 50 }
        };

            // 샘플 로케이션 데이터 정의
            var locations = new List<Location>
        {
            new Location { Id = 0, DistanceToExit = 1 },
            new Location { Id = 1, DistanceToExit = 2 },
            new Location { Id = 2, DistanceToExit = 3 },
            new Location { Id = 3, DistanceToExit = 4 },
            new Location { Id = 4, DistanceToExit = 5 }
        };

            int numSkus = skus.Count;
            int numLocs = locations.Count;

            // OR-Tools 모델 생성
            CpModel model = new CpModel();

            // SKU i번째 SKU가 j번째 로케이션에 배정되는지 여부를 나타내는 변수 생성
            BoolVar[,] assign = new BoolVar[numSkus, numLocs];
            for (int i = 0; i < numSkus; i++)
            {
                for (int j = 0; j < numLocs; j++)
                {
                    assign[i, j] = model.NewBoolVar($"sku_{i}_loc_{j}");
                }
            }

            // 제약 조건 추가

            // [조건 1]각 SKU는 정확히 하나의 로케이션에만 배정
            for (int i = 0; i < numSkus; i++)
            {
                LinearExpr skuAssigned = LinearExpr.Sum(Array.ConvertAll(assign.GetRow(i), b => (IntVar)b));
                model.Add(skuAssigned == 1);
            }

            // [조건 2]각 로케이션의 배정된 SKU의 총 무게가 최대 중량을 초과하지 않도록 제한
            for (int j = 0; j < numLocs; j++)
            {
                List<LinearExpr> weights = new List<LinearExpr>();
                for (int i = 0; i < numSkus; i++)
                {
                    weights.Add(assign[i, j] * skus[i].Weight);
                }
                model.Add(LinearExpr.Sum(weights) <= locations[j].MaxWeight);
            }

            // 목적 함수 : 자주 출고되는 SKU는 출구와 가까운 곳에 배치
            List<LinearExpr> weightedDistances = new List<LinearExpr>();
            for (int i = 0; i < numSkus; i++)
            {
                for (int j = 0; j < numLocs; j++)
                {
                    int cost = skus[i].Frequency * locations[j].DistanceToExit;
                    weightedDistances.Add(assign[i, j] * cost);
                }
            }

            // 거리 * 출고빈도 최소화 - 출고 빈도 별 가까운 로케이션 배정을 위한 목적
            model.Minimize(LinearExpr.Sum(weightedDistances));

            // 모델이 제약 및 조건을 바탕으로 결과
            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            // 결과 출력
            if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
            {
                Console.WriteLine("최적 배정 결과:");
                for (int i = 0; i < numSkus; i++)
                {
                    for (int j = 0; j < numLocs; j++)
                    {
                        if (solver.BooleanValue(assign[i, j]))
                        {
                            Console.WriteLine($"SKU {skus[i].Name} → 로케이션 {locations[j].Id} (거리: {locations[j].DistanceToExit})");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("해를 찾지 못했습니다.");
            }
        }
    }

    // 확장 메서드 행 추출을 위한 클래스
    public static class Extensions
    {
        public static T[] GetRow<T>(this T[,] array, int row)
        {
            int cols = array.GetLength(1);
            T[] result = new T[cols];
            for (int i = 0; i < cols; i++) result[i] = array[row, i];
            return result;
        }
    }
}
