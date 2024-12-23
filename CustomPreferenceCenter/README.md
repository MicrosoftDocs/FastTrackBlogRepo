# Creating a custom Preference Center in Power Pages

This folder contains the sample implementation for a custom preference center for Customer Insights - Journeys. For a high-level description, please look at the corresponding blog article at https://community.dynamics.com/blogs/post/?postid=3a361b7e-80b0-ee11-92bd-002248527d3d

The intention of this article and the sample implementation was to demonstrate how a custom preference center for CI-J can be created. This would allow you to 
- provide a familiar place for consent management for your customers 
- show all relevant contact points at once 
- display a 3-state checkbox (opt-in, opt-out, no information)

The following short [Demo Video](/CustomPreferenceCenter/assets/CustomPreferenceCenterDemo_final.mp4) shows the details and how the custom preference center works. 

The implemenation is divided into 3 parts: 
- the UI in Power Pages
- the API 
- the Backend processing through Azure Logic apps

# UI in Power Pages

Important to note is that for the consent management, a known user must be present for the preference center to work. In other words, the user must be logged in to Power Pages. 
On top of that, this solution is created in the following way: 
- fixed compliance profile: the rendered preference center requires the ID of one compliance profile, for which purposes and topics are shown.
- uses **emailaddress1** and **mobilephone** as channels: the preference center only shows contact point consent records for those two channels. If you want to show additional channels, the implementation would need to change to accomodate these requirements. 
- supports one commercial purpose in the complianc profile

The page link for managing consent is contained in the User Dropdown menu after the user has logged in to the site. 

![Consent Landing Page](/CustomPreferenceCenter/assets/Consent%20Link.jpg)

This menu item can easily created in Power Pages administration so that it shows up under the Profile link. Clicking the consent link will redirect the user to the preference center which is rendered dynamically. 

![Consent Landing Page](/CustomPreferenceCenter/assets/Consent%20Landing%20Page.jpg)

Here, the user can make changes and press the "Save Changes" button to submit the form back to CI-J. The script to render the form and submit the changes back is [here](/CustomPreferenceCenter/assets/customConsentManagement.html). The form that was created is able to determine which items have changed (dirty flag) and only submits those values which have changed. 


# the API part

The Power Pages script does not directly read or update Dataverse. Instead, an API is used to retrieve the data and update the consent. This is done this way because there are a couple of requests to be executed to retrieve the configuration for all purposes and topics for one compliance center. 
There are two APIs that have been implemented: 
- get-consent: get request which calls an Azure Logic app to read the compliance profile, its purposes and topics as well as the contact point consent records for this profile
- update-consent: post request to update contact point consent records for this contact according to the changes. 

Azure API Management takes care of the authentication and authorization. This is a crucial part for the security of the implementation and needs to be adapted according to the security requirements of your organization. The requests are redirected to the corresponding Azure Logic Apps. 

# the Backend part 

The access to Dataverse is done through Azure Logic apps. The Logic app for consent retrieval is shown in the following picture. 

![Consent Payload](/CustomPreferenceCenter/assets/getconsent_payload.jpg)

The final goal of this app is to construct a json payload that is sent back to the page to be consumed by the Javascript functions. This app works in the following way: 
- the contact id is used to read the values for **emailaddress1** and **mobilephone**.
- the contact point consent records (the stored consents) for these two entries (channels)
- prepare the skeleton for the return payload 
- fill in the details for text consent (= mobilephone) and email consent in parallel by iterating through the commercial topics and add them together with purpose entries to the payload.
- the procedure uses Javascript to determine the opt-in / opt-out status from the array of contact points that was read before. 

![Azure Logic app to retrieve consent](/CustomPreferenceCenter/assets/getconsentbycontact.jpg)

The second Logic app retrieves the submited values. For each of the changes, we need to check if an existing CPC record needs to be updated (cpc record id exists) or an new one has to be created. 

![Azure Logic app to upsert consent](/CustomPreferenceCenter/assets/updateconsent.jpg).

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.