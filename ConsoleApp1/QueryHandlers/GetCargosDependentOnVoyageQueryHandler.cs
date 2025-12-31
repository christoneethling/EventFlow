// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using ConsoleApp1.ReadModels;
using EventFlow.Core;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Queries;
using EventFlow.Queries;
using EventFlow.SQLite.Connections;

namespace ConsoleApp1.QueryHandlers
{
    public class GetCargosDependentOnVoyageQueryHandler : IQueryHandler<GetCargosDependentOnVoyageQuery, IReadOnlyCollection<Cargo>>
    {
        private readonly ISQLiteConnection _connection;

        public GetCargosDependentOnVoyageQueryHandler(ISQLiteConnection connection)
        {
            _connection = connection;
        }

        public async Task<IReadOnlyCollection<Cargo>> ExecuteQueryAsync(
            GetCargosDependentOnVoyageQuery query,
            CancellationToken cancellationToken)
        {
            var sql = @"
                SELECT * FROM [ReadModel-Cargo]
                WHERE DependentVoyageIds LIKE @VoyageId";

            var voyageIdPattern = $"%{query.VoyageId.Value}%";
            var parameters = new { VoyageId = voyageIdPattern };

            var cargoReadModels = await _connection.QueryAsync<CargoReadModel>(
                Label.Named("query-cargos-by-voyage"),
                string.Empty,
                cancellationToken,
                sql,
                parameters);

            return cargoReadModels
                .Where(rm => rm.DependentVoyageIds?.Split(',').Contains(query.VoyageId.Value) ?? false)
                .Select(rm => rm.ToCargo())
                .ToList();
        }
    }
}
