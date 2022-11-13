define(['globalize', 'loading', 'mainTabsManager','events',  'serverNotifications', 'components/taskbutton', 'datetime', 'paper-icon-button-light', 'formDialogStyle', 'emby-linkbutton', 'emby-collapse', 'emby-input', 'emby-select', 'emby-toggle'],
    function (globalize, loading, mainTabsManager, events, serverNotifications, taskButton, datetime) {

        var pluginId = "7E862375-3521-4A23-BA3E-8598EBAC4A50";
        
        function getTabs() {
            var tabs = [                           
                {
                    href: Dashboard.getConfigurationPageUrl('MovieLensRecommendationConfigurationPage'),
                    name: 'Recommendations'
                },
                {
                    href: Dashboard.getConfigurationPageUrl('MovieLensPluginSettingsConfigurationPage'),
                    name: "Settings"
                },
            ];            
            return tabs;
        }
        return function (view) {

            var config;

            const txtTrainingIterations = view.querySelector("#txtNumTrainingIterations");
            const txtDateLastPlayedLimit = view.querySelector("#txtNumLastPlayedLimit");
            const txtNumRecommendationPredictionThreshold  = view.querySelector('#txtNumRecommendationPredictionThreshold');
            const chkEnableFavorRecentlyAdded = view.querySelector('#chkEnableFavorRecentlyAdded');
            const chkEnableFavorNewReleases = view.querySelector('#chkEnableFavorNewReleases');
            const lastTrained = view.querySelector('.lastTrained');
            const txtRecommendationMaxNumber = view.querySelector('#txtNumPredictions');

            chkEnableFavorNewReleases.addEventListener('change', async (e) => {
                config = await ApiClient.getPluginConfiguration(pluginId);
                config.FavorNewReleases = chkEnableFavorNewReleases.checked;
                var result = await ApiClient.updatePluginConfiguration(pluginId, config);

                Dashboard.processPluginConfigurationUpdateResult(result);
            });

            chkEnableFavorRecentlyAdded.addEventListener('change', async (e) => {
                config = await ApiClient.getPluginConfiguration(pluginId);
                config.FavorRecentlyAdded = chkEnableFavorRecentlyAdded.checked;
                var result = await ApiClient.updatePluginConfiguration(pluginId, config);

                Dashboard.processPluginConfigurationUpdateResult(result);
            });

            txtRecommendationMaxNumber.addEventListener("input", async (e) => {
                
                config = await ApiClient.getPluginConfiguration(pluginId);
                config.RecommendationMaxNumber = e.target.value;
                var result = await ApiClient.updatePluginConfiguration(pluginId, config);

                Dashboard.processPluginConfigurationUpdateResult(result);

            });

            txtTrainingIterations.addEventListener("input", async (e) => {
                
                config = await ApiClient.getPluginConfiguration(pluginId);
                config.TrainingIterations = e.target.value;
                var result = await ApiClient.updatePluginConfiguration(pluginId, config);

                Dashboard.processPluginConfigurationUpdateResult(result);

            });

            txtDateLastPlayedLimit.addEventListener("input", async (e) => {
                
                config = await ApiClient.getPluginConfiguration(pluginId);
                config.LastPlayedMonths = e.target.value;
                var result = await ApiClient.updatePluginConfiguration(pluginId, config);

                Dashboard.processPluginConfigurationUpdateResult(result);

            });

            txtNumRecommendationPredictionThreshold.addEventListener("input", async (e) => {
                
                config = await ApiClient.getPluginConfiguration(pluginId);
                config.MaxRecommendationPredictionThreshold = e.target.value;
                var result = await ApiClient.updatePluginConfiguration(pluginId, config);

                Dashboard.processPluginConfigurationUpdateResult(result);

            });

            view.addEventListener('viewshow',
                async function () {

                    loading.show();

                    mainTabsManager.setTabs(this, 1, getTabs);

                    config = await ApiClient.getPluginConfiguration(pluginId);

                    txtTrainingIterations.value = config.TrainingIterations;
                    txtDateLastPlayedLimit.value = config.LastPlayedMonths;
                    txtNumRecommendationPredictionThreshold.value = config.MaxRecommendationPredictionThreshold;
                    chkEnableFavorNewReleases.checked = config.FavorNewReleases;
                    chkEnableFavorRecentlyAdded.checked = config.FavorRecentlyAdded;

                    if (config.LastTrainedDate) {
                        lastTrained.innerHTML = "Neural Network was last trained on: " + datetime.parseISO8601Date(config.LastTrainedDate);
                    } else {
                        lastTrained.innerHTML = "The Neural Network has not been trained."
                    }

                    if (taskButton) {
                        taskButton.default({
                            mode: 'on',
                            progressElem: view.querySelector('.itemProgressBar'),
                            panel: view.querySelector('.taskProgress'),
                            taskKey: 'TrainMovieRecommendationModel',
                            button: view.querySelector('.btnTraining')
                        });
                    }

                    if (taskButton) {
                        taskButton.default({
                            mode: 'on',
                            progressElem: view.querySelector('.itemProgressBar'),
                            panel: view.querySelector('.taskProgress'),
                            taskKey: 'PredictMovieRecommendationModel',
                            button: view.querySelector('.btnPredictions')
                        });
                    }

                    loading.hide();
                })
        }
    })