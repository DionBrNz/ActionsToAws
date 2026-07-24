using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;
using System.Collections.Generic;

namespace AccountProcessingInfra;

public class GithubPermissions : Stack
{
    public GithubPermissions(Construct scope = null, string id = null, IStackProps props = null) : base(scope, id,
        props)
    {
        var githubOidc = new OpenIdConnectProvider(this, "GithubOidc", new OpenIdConnectProviderProps
        {
            Url = "https://token.actions.githubusercontent.com",
            ClientIds = new[] { "sts.amazonaws.com" }
        });

        var role = new Role(this, "GithubActionsRole", new RoleProps
        {
            AssumedBy = new FederatedPrincipal(
                githubOidc.OpenIdConnectProviderArn,
                new Dictionary<string, object>
                {
                    {
                        "StringEquals", new Dictionary<string, object>
                        {
                            { "token.actions.githubusercontent.com:aud", "sts.amazonaws.com" }
                        }
                    },
                    {
                        "StringLike", new Dictionary<string, object>
                        {
                            // Replace with your actual org/repo
                            { "token.actions.githubusercontent.com:sub", "repo:DionBrNz/ActionsToAws:*" }
                        }
                    }
                },
                "sts:AssumeRoleWithWebIdentity"
            ),

            // Optional: attach policies here
            InlinePolicies = new Dictionary<string, PolicyDocument>
            {
                {
                    "DefaultPermissions",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements =
                        [
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Actions = ["s3:ListAllMyBuckets"],
                                Resources =  ["*"]
                            })
                        ]
                    })
                }
            }
        });
    }
}