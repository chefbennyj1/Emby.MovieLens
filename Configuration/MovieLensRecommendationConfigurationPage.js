define(['globalize', 'loading', 'mainTabsManager','events',  'serverNotifications', 'components/taskbutton', 'datetime', 'paper-icon-button-light', 'formDialogStyle', 'emby-linkbutton', 'emby-collapse', 'emby-input', 'emby-select', 'emby-toggle', 'emby-button'],
    function (globalize, loading, mainTabsManager, events, serverNotifications, taskButton, datetime) {

        ApiClient.getUsers = function () {
            var url = this.getUrl("Users");
            return this.getJSON(url);
        }
        ApiClient.getRecommendations = function() {
            var url = this.getUrl("Recommendations");
            return this.getJSON(url);
        }

        ApiClient.getUserRecommendations = function(id) {
            var url = this.getUrl("Recommendations/" + id);
            return this.getJSON(url);
        }

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

            const resultBody = view.querySelector('.resultBody');
            const userContainer = view.querySelector('.userButtonsContainer');

            view.addEventListener('viewshow',
                async function () {

                    loading.show();

                    mainTabsManager.setTabs(this, 0, getTabs);
                    var userResult = await ApiClient.getUsers();
                    userResult.forEach(user => {
                        userContainer.innerHTML += '<button is="emby-button" class="userButton raised emby-button emby-button-backdropfilter raised-backdropfilter" id="' + user.Id + '">' + user.Name + '</button>'
                    });

                    view.querySelectorAll('.userButton').forEach(btn => {
                        btn.addEventListener('click', async e => {
                            var id = e.target.id;
                            var recommendationResults = await ApiClient.getUserRecommendations(id);
                            var html = '';

                            recommendationResults.forEach(result => {
                                html += '<tr class="detailTableBodyRow detailTableBodyRow-shaded">';
                                html += '<td class="detailTableBodyCell fileCell">';
                                html += result.UserName
                                html += '</td>';
                                html += '<td class="detailTableBodyCell fileCell">';
                                html += result.MovieName
                                html += '</td>';
                                html += '<td class="detailTableBodyCell fileCell">';
                                html += result.Score
                                html += '</td>';
                                html += '</tr>'

                            })
                            resultBody.innerHTML = html;
                        })
                    })
                    
                    loading.hide();
                });
        
        }
    });