
        class FormField {
        constructor(element, errorSpan) {
            this.element = element;
            this.required = element.required;
            this.validationMessage = '';
            this.validField = true;
            this.errorSpan = errorSpan;
        }
        getValue() {
            return this.element.value;
        }
        setValue(value) {
            this.element.value = value;
        }
        clearValue() {
            this.element.value = '';
        }
        showError() {
            this.element.classList.add('errorField'); // add an errorField class to the field. You can create your own definition or errorField to change the border or bgcolor of field in error state.
            this.errorSpan.textContent = this.validationMessage; // set the error message text content
            this.errorSpan.style.display = 'block'; // show the error message span
        }
        hideError() {
            this.element.classList.remove('errorField'); // remove the errorField class if the field is valid
            this.errorSpan.textContent = ''; // clear the error message text content
            this.errorSpan.style.display = 'none'; // hide the error message span
        }
        validate() {
            if (this.required && this.element.type === "checkbox") {
                if (!this.element.checked) {
                    this.showError();
                    console.log(this.element.name + " checkbox not checked"); // log error message and prevent form submission
                    return false;
                }
                else {
                    this.hideError();
                    return true;
                }
            }
            else {
                if ((this.required && !this.element.value) || !this.element.validity.valid) {
                    this.showError();
                    console.log(this.element.name + " value is not valid"); // log error message and prevent form submission
                    return false;
                }
                else {
                    this.hideError();
                    return true;
                }
            }
        }    
        }
        const form = document.querySelector('.marketingForm'); // select the main form element
        const allFormFields = form.querySelectorAll('form input, form select, form textarea, form checkbox'); // select all fields in the form
        const formAllFieldArray = [];
        
        allFormFields.forEach((field) => {
            const span = document.createElement('span'); // span element will be added to each field to show the error message
            span.style.color = "red"; // set error message font color.
            var errorSpan;
            if (field.type === "checkbox" || field.type === "tel") {
                errorSpan = field.parentNode.insertAdjacentElement('afterend', span); // Add the span element after the form field
                field.addEventListener('click', () => {
                    formField.validate(); // add event listener for checkbox is clicked event
                });
            }
            else {
                errorSpan = field.parentNode.insertBefore(span, field.nextSibling); // Add the span element after the form field
                field.addEventListener('input', () => {
                    formField.validate(); // add event listener for inputing a value event
                });
            }              
            
            const formField = new FormField(field, errorSpan);
            // *** DEFINE ERROR MESSAGES HERE ***
            if (field.name === "firstname") formField.validationMessage = 'Please tell us how we can call you.'; // error message for the first name field
            else if (field.name === "lastname") formField.validationMessage = 'We would really like to know your last name.'; // error message for the last name field
            else if (field.name === "emailaddress1") formField.validationMessage = 'Do you really want us to send to this address?'; // error message for the email field
            else if (field.name === "mobilephone") formField.validationMessage = 'Seems like there is a typo in the phone number. It should start with "+", with at least 4 digits'; // error message for the phone field
            // *** DEFAULT error messages for FIELD TYPES ***
            else if (field.type === "checkbox") formField.validationMessage = 'Please allow us to send you emails.'; // DEFAULT error message for checkbox
            else formField.validationMessage = 'Enter a valid value'; // DEFAULT error message for all other types of fields
            
            formAllFieldArray.push(formField); // add newly created instance of field class to the array
        });
        form.setAttribute('novalidate', true); // disable default browser validation        
        form.addEventListener('d365mkt-formsubmit', (event) => { // validate form submission
            let isFormValid = true; // assume the form is valid by default
            formAllFieldArray.forEach((field) => {
              if(!field.validate()) {
                isFormValid = false // form is not valid
              }
            });
            if(!isFormValid) {
                event.preventDefault(); // Prevent the form from submitting
                console.log("Form submission is not valid"); // log error message
            }
        });        
   