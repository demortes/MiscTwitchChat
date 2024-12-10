using MiscTwitchChat.ClassLib;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Text.Json;
using System;
using MiscTwitchChat.Classlib.Entities;
using System.Runtime.ExceptionServices;

namespace MiscTwitchChat.Classlib
{
    public class GoFundMeService : IGoFundMeService
    {
        private readonly string ENDPOINT = "https://graphql.gofundme.com/graphql";
        public async Task<Donations> GetDonations(string slug, string? cursor, int? first, int? last)
        {
            var client = new GraphQLHttpClient(ENDPOINT, new NewtonsoftJsonSerializer());
            var request = new GraphQLRequest()
            {
                Query = @"query GetDonations($slug: ID!, $first: Int, $after: String, $last: Int) {
                            fundraiser(slug: $slug) {
                                donations(first: $first, after: $after, last: $last) {
                                    edges {
                                        cursor,
                                        node {
                                            id
                                            name
                                            createdAt
                                            amount {
                                                amount
                                                currencyCode
                                            }
                                        }
                                    }
                                }
                            }
                        }",
                OperationName = "GetDonations",
                Variables = new
                {
                    slug,
                    first,
                    last,
                    after = cursor //TODO: This doesn't restrict it properly for some reason, figure it out later.
                }
            };

            var response = await client.SendQueryAsync<FundraiserData>(request);
            var rawResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            return response.Data.fundraiser.donations;
        }
    }
}
