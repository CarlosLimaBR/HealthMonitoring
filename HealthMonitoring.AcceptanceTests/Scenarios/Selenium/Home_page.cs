﻿using LightBDD;

namespace HealthMonitoring.AcceptanceTests.Scenarios.Selenium
{
    [FeatureDescription(
@"In order to understend how to use HealthMoniting UI
As User 
I want to open home page")]
    public partial class Home_page
    {
        [Scenario]
        public void Verification_of_page_title()
        {
            Runner.RunScenario(
                _ => Given_home_page(),
                _ => Then_page_should_contain_title()
                );
        }

        [Scenario]
        public void Verification_of_dashboard_link()
        {
            Runner.RunScenario(
                _ => Given_home_page(),
                _ => When_user_clicks_on_dashboad_page_link(),
                _ => Then_dashboard_page_should_be_opened()
                );
        }

        [Scenario]
        public void Verification_of_swagger_link()
        {
            Runner.RunScenario(
                _ => Given_home_page(),
                _ => When_user_clicks_on_swagger_page_link(),
                _ => Then_swagger_page_should_be_opened()
                );
        }

        [Scenario]
        public void Verification_of_project_link()
        {
            Runner.RunScenario(
                _ => Given_home_page(),
                _ => When_user_clicks_on_project_page_link(),
                _ => Then_project_page_should_be_opened()
                );
        }

        [Scenario]
        public void Applying_status_filter_to_endpoints()
        {
            Runner.RunScenario(
                _ => Given_home_page(),
                _ => When_user_clicks_on_status_button(),
                _ => Then_only_endpoints_with_chosen_status_should_be_shown(),
                _ => Then_should_be_shown_selected_status(),
                _ => Then_status_filter_should_be_appended_to_url()
                );
        }

        [Scenario]
        public void Applying_tag_filter_to_endpoints()
        {
            Runner.RunScenario(
                _ => Given_home_page(),
                _ => When_user_clicks_on_endpoint_tags(),
                _ => Then_only_endpoints_with_chosen_tags_should_be_shown(),
                _ => Then_should_be_shown_which_tags_are_selected(),
                _ => Then_tag_filter_should_be_appended_to_url()
                );
        }

        [Scenario]
        public void Applying_url_filter_to_endpoints()
        {
            Runner.RunScenario(
                _ => When_user_navigates_to_home_page_with_filters_in_url(),
                _ => Then_only_endpoints_with_chosen_parameters_should_be_shown()
                );
        }
    }
}